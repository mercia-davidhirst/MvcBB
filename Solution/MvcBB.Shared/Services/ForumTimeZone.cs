using System.Collections.Concurrent;

namespace MvcBB.Shared.Services
{
    /// <summary>
    /// The forum's display time zone (Admin &gt; Settings &gt; Time Zone,
    /// DisplaySettings.TimeZoneId). Every timestamp is stored in UTC; this is
    /// the one place that converts between UTC and the forum's wall-clock
    /// time, for display and for admin-entered dates (announcement windows,
    /// account locks).
    ///
    /// Ids are stored in IANA form ("Europe/London") where the platform can
    /// convert to one, since IANA ids work on both Windows and Linux hosts on
    /// .NET 6+. A Windows id ("GMT Standard Time") saved by hand still
    /// resolves. A blank id means "the server's own local time zone", which is
    /// what MvcBB did before the setting existed, so existing installs look
    /// the same until an admin picks a zone.
    /// </summary>
    public static class ForumTimeZone
    {
        public const int MaxIdLength = 100;

        private static readonly ConcurrentDictionary<string, TimeZoneInfo?> Cache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The zone for a stored id. Blank, or an id this host can't resolve,
        /// falls back to the server's local zone (the pre-1.1.0 behaviour), so a
        /// bad value can never break date rendering.
        /// </summary>
        public static TimeZoneInfo Resolve(string? timeZoneId) =>
            TryResolve(timeZoneId) ?? TimeZoneInfo.Local;

        /// <summary>True for blank (server local time) or any id this host can resolve.</summary>
        public static bool IsValidId(string? timeZoneId) =>
            string.IsNullOrWhiteSpace(timeZoneId)
            || (timeZoneId.Length <= MaxIdLength && TryResolve(timeZoneId) != null);

        /// <summary>
        /// The id to store for a zone the admin picked: IANA when the platform
        /// can convert to it, otherwise the id as given. Blank stays blank.
        /// </summary>
        public static string NormalizeId(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                return string.Empty;
            }

            var id = timeZoneId.Trim();
            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId))
            {
                return ianaId;
            }

            return id;
        }

        /// <summary>
        /// Converts a stored (UTC) timestamp to forum wall-clock time. The
        /// result's Kind is Unspecified, since it's neither UTC nor this
        /// server's local time.
        /// </summary>
        /// <remarks>
        /// DateTimeKind handling: Utc is used as is; Unspecified is treated as
        /// UTC (that's what SQL Server's DATETIME2 columns and JSON without an
        /// offset come back as); Local (e.g. JSON with a "+01:00" offset, which
        /// System.Text.Json turns into server-local time) is converted to UTC
        /// first.
        /// </remarks>
        public static DateTime ToForumTime(DateTime utcDateTime, TimeZoneInfo timeZone)
        {
            var utc = AsUtc(utcDateTime);
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone), DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Converts a wall-clock time entered in the forum's zone (e.g. from a
        /// datetime-local input) to UTC for storage. A value already marked Utc
        /// is returned unchanged; a Local one is converted from this server's
        /// zone. A time that doesn't exist in the forum's zone (the hour skipped
        /// when the clocks go forward) is moved forward past the gap; an
        /// ambiguous one (the repeated hour when they go back) is read as
        /// standard time.
        /// </summary>
        public static DateTime ToUtc(DateTime forumDateTime, TimeZoneInfo timeZone)
        {
            switch (forumDateTime.Kind)
            {
                case DateTimeKind.Utc:
                    return forumDateTime;
                case DateTimeKind.Local:
                    return forumDateTime.ToUniversalTime();
            }

            var local = DateTime.SpecifyKind(forumDateTime, DateTimeKind.Unspecified);
            if (timeZone.IsInvalidTime(local))
            {
                // Step forward in 15-minute increments (covers every real DST
                // gap, including 30-minute ones) until the time exists.
                var adjusted = local;
                for (var i = 0; i < 16 && timeZone.IsInvalidTime(adjusted); i++)
                {
                    adjusted = adjusted.AddMinutes(15);
                }
                local = adjusted;
            }

            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), DateTimeKind.Utc);
        }

        /// <summary>Forum wall-clock time right now.</summary>
        public static DateTime Now(TimeZoneInfo timeZone) => ToForumTime(DateTime.UtcNow, timeZone);

        /// <summary>
        /// Every zone this host knows, for the admin dropdown: (id to store,
        /// label). Ids are IANA where the platform can convert to one; zones
        /// that map to the same IANA id appear once. In the platform's order
        /// (by UTC offset).
        /// </summary>
        public static IReadOnlyList<(string Id, string DisplayName)> GetOptions()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var options = new List<(string Id, string DisplayName)>();
            foreach (var zone in TimeZoneInfo.GetSystemTimeZones())
            {
                var id = NormalizeId(zone.Id);
                if (seen.Add(id))
                {
                    var label = string.Equals(id, zone.Id, StringComparison.OrdinalIgnoreCase)
                        ? zone.DisplayName
                        : $"{zone.DisplayName} - {id}";
                    options.Add((id, label));
                }
            }

            return options;
        }

        private static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        private static TimeZoneInfo? TryResolve(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                return null;
            }

            return Cache.GetOrAdd(timeZoneId.Trim(), static id =>
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                {
                    return zone;
                }

                // Older .NET/ICU combinations can miss the automatic IANA <->
                // Windows mapping, so try the explicit conversion both ways.
                if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId)
                    && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out zone))
                {
                    return zone;
                }

                if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId)
                    && TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out zone))
                {
                    return zone;
                }

                return null;
            });
        }
    }
}
