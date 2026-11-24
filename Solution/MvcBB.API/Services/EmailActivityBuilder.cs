using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Services;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Turns the send log into the Sending activity chart's data. Buckets
    /// follow the forum's time zone: the 24-hour view has one bar per local
    /// clock hour plus a rolling 60-minute line every 10 minutes; the 7- and
    /// 30-day views have one bar per local day.
    /// </summary>
    public static class EmailActivityBuilder
    {
        public const string Last24Hours = "24h";
        public const string Last7Days = "7d";
        public const string Last30Days = "30d";

        private const int SlotMinutes = 10;

        public static string NormalizeRange(string? range) => range switch
        {
            Last7Days => Last7Days,
            Last30Days => Last30Days,
            _ => Last24Hours
        };

        public static EmailActivity Build(string? range, IEmailSendLogRepository sendLog, EmailSendUsage usage, TimeZoneInfo timeZone, DateTime nowUtc)
        {
            range = NormalizeRange(range);
            return range == Last24Hours
                ? BuildHourly(sendLog, usage, timeZone, nowUtc)
                : BuildDaily(range, range == Last7Days ? 7 : 30, sendLog, usage, timeZone, nowUtc);
        }

        private static EmailActivity BuildHourly(IEmailSendLogRepository sendLog, EmailSendUsage usage, TimeZoneInfo timeZone, DateTime nowUtc)
        {
            // The 24 local clock hours up to the end of the current one. Stepping
            // in UTC hours from a local hour boundary keeps every bar on a local
            // hour, half-hour time zones included.
            var nowLocal = ForumTimeZone.ToForumTime(nowUtc, timeZone);
            var endLocal = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, nowLocal.Hour, 0, 0).AddHours(1);
            var endUtc = ForumTimeZone.ToUtc(endLocal, timeZone);
            var startUtc = endUtc.AddHours(-24);

            // 10-minute slots from an hour before the first bar, so the rolling
            // line is right from its first point.
            var slotsFrom = startUtc.AddHours(-1);
            var slotCount = (int)((endUtc - slotsFrom).TotalMinutes / SlotMinutes);
            var slots = new EmailActivityBucket[slotCount];
            for (var i = 0; i < slotCount; i++)
            {
                slots[i] = new EmailActivityBucket { StartUtc = slotsFrom.AddMinutes(i * SlotMinutes), EndUtc = slotsFrom.AddMinutes((i + 1) * SlotMinutes) };
            }
            foreach (var row in sendLog.GetCounts(slotsFrom, endUtc, SlotMinutes))
            {
                var index = (int)Math.Round((row.BucketStart - slotsFrom).TotalMinutes / SlotMinutes);
                if (index >= 0 && index < slotCount)
                    slots[index].Add(row.Kind, row.Count);
            }

            var activity = new EmailActivity { Range = Last24Hours, Hourly = true, Usage = usage };
            const int slotsPerHour = 60 / SlotMinutes;
            for (var hour = 0; hour < 24; hour++)
            {
                var bucket = new EmailActivityBucket { StartUtc = startUtc.AddHours(hour), EndUtc = startUtc.AddHours(hour + 1) };
                for (var s = 0; s < slotsPerHour; s++)
                {
                    AddInto(bucket, slots[slotsPerHour * (hour + 1) + s]);
                }
                activity.Buckets.Add(bucket);
            }

            // Rolling 60-minute total at the end of each 10-minute slot, up to now.
            for (var i = slotsPerHour; i < slotCount; i++)
            {
                var at = slots[i].EndUtc;
                if (at > nowUtc.AddMinutes(SlotMinutes))
                    break;
                var total = 0;
                for (var s = i - slotsPerHour + 1; s <= i; s++)
                {
                    total += slots[s].Total;
                }
                activity.RollingHour.Add(new EmailRollingPoint { AtUtc = at, Count = total });
            }

            return activity;
        }

        private static EmailActivity BuildDaily(string range, int days, IEmailSendLogRepository sendLog, EmailSendUsage usage, TimeZoneInfo timeZone, DateTime nowUtc)
        {
            var today = ForumTimeZone.ToForumTime(nowUtc, timeZone).Date;
            var activity = new EmailActivity { Range = range, Hourly = false, Usage = usage };
            for (var d = days - 1; d >= 0; d--)
            {
                var dayLocal = today.AddDays(-d);
                activity.Buckets.Add(new EmailActivityBucket
                {
                    StartUtc = ForumTimeZone.ToUtc(dayLocal, timeZone),
                    EndUtc = ForumTimeZone.ToUtc(dayLocal.AddDays(1), timeZone)
                });
            }

            // Counted per hour, then each hour put in its local day - days
            // aren't always 24 hours long when the clocks change.
            var fromUtc = activity.Buckets[0].StartUtc;
            var toUtc = activity.Buckets[^1].EndUtc;
            foreach (var row in sendLog.GetCounts(fromUtc, toUtc, 60))
            {
                var bucket = activity.Buckets.FirstOrDefault(b => row.BucketStart >= b.StartUtc && row.BucketStart < b.EndUtc);
                bucket?.Add(row.Kind, row.Count);
            }

            return activity;
        }

        private static void AddInto(EmailActivityBucket target, EmailActivityBucket source)
        {
            foreach (var kind in Enum.GetValues<EmailKind>())
            {
                target.Add(kind, source.CountOf(kind));
            }
        }
    }
}
