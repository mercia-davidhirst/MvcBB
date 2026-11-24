using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.Services
{
    /// <summary>The host's sending limits in force. 0 = no limit.</summary>
    public readonly record struct SendLimits(int PerHour, int PerDay)
    {
        public bool Any => PerHour > 0 || PerDay > 0;

        /// <summary>
        /// The limits set in Admin &gt; Settings (DisplaySettings.MaxEmailsPerHour /
        /// MaxEmailsPerDay) win; each one left at 0 there falls back to
        /// AdminEmail:MaxPerHour / MaxPerDay in appsettings.json.
        /// </summary>
        public static SendLimits Resolve(AdminEmailOptions options, DisplaySettings? settings) => new(
            settings?.MaxEmailsPerHour > 0 ? settings.MaxEmailsPerHour : Math.Max(0, options.MaxPerHour),
            settings?.MaxEmailsPerDay > 0 ? settings.MaxEmailsPerDay : Math.Max(0, options.MaxPerDay));
    }

    /// <summary>How a send would go under the limits: how many go straight away, and roughly how many hours until the last one can (null if all go now).</summary>
    public readonly record struct SendEstimate(int CanSendNow, double? Hours);

    /// <summary>
    /// How the host's sending limits apply to admin emails. Part of each
    /// limit is held back for the site's own emails (password resets,
    /// registration, notifications), so a big admin send can never use the
    /// whole allowance and leave a member unable to reset their password.
    /// </summary>
    public static class AdminEmailRateLimit
    {
        public static readonly TimeSpan Hour = TimeSpan.FromHours(1);
        public static readonly TimeSpan Day = TimeSpan.FromHours(24);

        /// <summary>The longest single wait before re-checking, so a pause can't be overslept.</summary>
        public static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(5);

        /// <summary>
        /// How much of a limit is kept for the site's own emails: 5%, at least 1.
        /// A limit of 1 keeps nothing back, or admin emails could never send.
        /// </summary>
        public static int Reserve(int limit) =>
            limit <= 1 ? 0 : Math.Min(Math.Max(1, limit / 20), limit - 1);

        /// <summary>How many admin emails a limit allows in its window (the limit less the reserve). 0 for no limit.</summary>
        public static int AdminShare(int limit) => limit <= 0 ? 0 : limit - Reserve(limit);

        /// <summary>
        /// How many admin emails may go out right now under both limits, or
        /// null when neither limit is set.
        /// </summary>
        public static int? Allowance(SendLimits limits, EmailSendCounter counter, DateTime nowUtc)
        {
            int? allowance = null;
            if (limits.PerHour > 0)
            {
                allowance = AdminShare(limits.PerHour) - counter.CountSince(nowUtc - Hour);
            }
            if (limits.PerDay > 0)
            {
                var daily = AdminShare(limits.PerDay) - counter.CountSince(nowUtc - Day);
                allowance = allowance == null ? daily : Math.Min(allowance.Value, daily);
            }
            return allowance == null ? null : Math.Max(0, allowance.Value);
        }

        /// <summary>
        /// After Allowance came back 0: when sending can next carry on, i.e.
        /// when the oldest send counted against each full limit leaves its
        /// window (the later of the two if both are full). Always after nowUtc.
        /// Shown to the admin as PausedUntil; the dispatcher still re-checks at
        /// least every MaxWait.
        /// </summary>
        public static DateTime ResumeTime(SendLimits limits, EmailSendCounter counter, DateTime nowUtc)
        {
            var resume = nowUtc.AddSeconds(1);
            if (limits.PerHour > 0 && counter.CountSince(nowUtc - Hour) >= AdminShare(limits.PerHour))
            {
                var oldest = counter.OldestSince(nowUtc - Hour);
                if (oldest.HasValue && oldest.Value + Hour > resume)
                    resume = oldest.Value + Hour;
            }
            if (limits.PerDay > 0 && counter.CountSince(nowUtc - Day) >= AdminShare(limits.PerDay))
            {
                var oldest = counter.OldestSince(nowUtc - Day);
                if (oldest.HasValue && oldest.Value + Day > resume)
                    resume = oldest.Value + Day;
            }
            return resume;
        }

        /// <summary>
        /// Roughly how many hours sending to recipientCount members takes under
        /// the limits, from an empty count; null when it fits in one go (or
        /// there's no limit). Ignores BatchDelaySeconds and other site emails.
        /// </summary>
        public static double? EstimatedHours(SendLimits limits, int recipientCount)
        {
            var now = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Estimate(limits, Array.Empty<DateTime>(), now, recipientCount).Hours;
        }

        /// <summary>
        /// How sending to recipientCount members would go from now, given the
        /// emails already sent (recentSendsUtc, oldest first - e.g. from
        /// EmailSendCounter.SentSince): how many can go straight away, and
        /// roughly how many hours until the last one can, as earlier sends drop
        /// out of the rolling hour/day. Hours is null when the whole send can go
        /// now (or there's no limit). Ignores BatchDelaySeconds and any other
        /// site emails sent meanwhile.
        /// </summary>
        public static SendEstimate Estimate(SendLimits limits, IReadOnlyList<DateTime> recentSendsUtc, DateTime nowUtc, int recipientCount)
        {
            if (!limits.Any || recipientCount <= 0)
                return new SendEstimate(Math.Max(0, recipientCount), null);

            // Replays the dispatcher: send as many as the limits allow at each
            // moment, then jump to when the oldest send holding a limit full
            // drops out of its window. Sends made at the same moment leave the
            // window together, so this takes a step per batch, not per email.
            var sends = recentSendsUtc.Where(t => t > nowUtc - Day).OrderBy(t => t).ToList();
            var time = nowUtc;
            var remaining = recipientCount;
            int? canSendNow = null;
            for (var step = 0; remaining > 0 && step < 100_000; step++)
            {
                var allowance = int.MaxValue;
                var next = DateTime.MaxValue;
                foreach (var (limit, window) in new[] { (limits.PerHour, Hour), (limits.PerDay, Day) })
                {
                    if (limit <= 0)
                        continue;
                    var from = FirstAfter(sends, time - window);
                    var free = AdminShare(limit) - (sends.Count - from);
                    allowance = Math.Min(allowance, Math.Max(0, free));
                    if (free <= 0 && from < sends.Count)
                    {
                        // Enough of the oldest must leave the window to free one slot.
                        var leaving = sends[Math.Min(sends.Count - 1, from - free)] + window;
                        next = next == DateTime.MaxValue || leaving > next ? leaving : next;
                    }
                }

                var batch = Math.Min(allowance, remaining);
                canSendNow ??= batch;
                if (batch > 0)
                {
                    sends.AddRange(Enumerable.Repeat(time, batch));
                    remaining -= batch;
                    continue;
                }
                if (next == DateTime.MaxValue)
                    break;
                time = next > time ? next : time.AddSeconds(1);
            }

            var hours = remaining > 0 || time > nowUtc ? (time - nowUtc).TotalHours : 0;
            return new SendEstimate(canSendNow ?? 0, hours > 0 ? hours : null);
        }

        // Index of the first send after cutoff (sends is in time order).
        private static int FirstAfter(List<DateTime> sends, DateTime cutoff)
        {
            int low = 0, high = sends.Count;
            while (low < high)
            {
                var mid = (low + high) / 2;
                if (sends[mid] > cutoff) high = mid; else low = mid + 1;
            }
            return low;
        }

        /// <summary>Emails sent in the last hour and day against the limits, and what that leaves for admin emails now.</summary>
        public static EmailSendUsage Usage(SendLimits limits, EmailSendCounter counter, DateTime nowUtc)
        {
            var allowance = Allowance(limits, counter, nowUtc);
            return new EmailSendUsage
            {
                SentLastHour = counter.CountSince(nowUtc - Hour),
                SentLast24Hours = counter.CountSince(nowUtc - Day),
                LimitPerHour = limits.PerHour,
                LimitPerDay = limits.PerDay,
                ReservePerHour = Reserve(limits.PerHour),
                ReservePerDay = Reserve(limits.PerDay),
                AdminAllowanceNow = allowance,
                NextFreeAtUtc = allowance == 0 ? ResumeTime(limits, counter, nowUtc) : null
            };
        }
    }
}
