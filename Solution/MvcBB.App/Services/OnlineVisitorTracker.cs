using System.Collections.Concurrent;

namespace MvcBB.App.Services
{
    public enum OnlineVisitorKind
    {
        Member,
        Guest,
        Bot
    }

    /// <summary>What the "Who's online" card shows: counts for one time window.</summary>
    public sealed class OnlineVisitorsSnapshot
    {
        public int WindowMinutes { get; init; }
        public int Members { get; init; }
        public int Guests { get; init; }
        public int Bots { get; init; }
        public int Total => Members + Guests + Bots;

        /// <summary>Each bot name seen in the window with how many of it (e.g. Google, 3), most first.</summary>
        public IReadOnlyList<KeyValuePair<string, int>> BotNames { get; init; } = Array.Empty<KeyValuePair<string, int>>();
    }

    /// <summary>
    /// Remembers who has loaded a page recently, for the home page "Who's
    /// online" card. Kept in the App's memory (a singleton), so it starts
    /// empty after a restart and each App instance counts only its own
    /// visitors. Nothing is written to the database.
    ///
    /// Members are keyed by user id, guests by a hash of their IP address
    /// and user agent (no cookie is set), and bots by name and IP address.
    /// Entries older than <see cref="MaxWindow"/> are pruned as it goes.
    /// </summary>
    public sealed class OnlineVisitorTracker
    {
        /// <summary>The longest online window the admin setting allows.</summary>
        public static readonly TimeSpan MaxWindow = TimeSpan.FromMinutes(60);

        // A ceiling on remembered visitors, so a flood of made-up user agents
        // can't grow this without limit. New guests and bots past it are not
        // counted until older entries expire; members always are.
        internal const int MaxEntries = 50_000;

        private static readonly TimeSpan PruneInterval = TimeSpan.FromMinutes(1);

        private readonly ConcurrentDictionary<string, Entry> _visitors = new(StringComparer.Ordinal);
        private long _lastPruneTicks;

        private readonly record struct Entry(OnlineVisitorKind Kind, string? BotName, DateTime LastSeenUtc);

        public int Count => _visitors.Count;

        public void RecordMember(string userId, string? guestKey, DateTime nowUtc)
        {
            Record("m:" + userId, new Entry(OnlineVisitorKind.Member, null, nowUtc), nowUtc, always: true);

            // The same browser was a guest a moment ago (they've just signed
            // in): stop counting it twice.
            if (guestKey != null)
            {
                _visitors.TryRemove("g:" + guestKey, out _);
            }
        }

        public void RecordGuest(string guestKey, DateTime nowUtc) =>
            Record("g:" + guestKey, new Entry(OnlineVisitorKind.Guest, null, nowUtc), nowUtc, always: false);

        public void RecordBot(string botName, string? ipAddress, DateTime nowUtc) =>
            Record("b:" + botName + "|" + ipAddress, new Entry(OnlineVisitorKind.Bot, botName, nowUtc), nowUtc, always: false);

        /// <summary>A member signed out: stop counting them as online straight away.</summary>
        public void RemoveMember(string userId) => _visitors.TryRemove("m:" + userId, out _);

        public OnlineVisitorsSnapshot GetSnapshot(int windowMinutes, DateTime nowUtc)
        {
            windowMinutes = Math.Clamp(windowMinutes, 1, (int)MaxWindow.TotalMinutes);
            var since = nowUtc.AddMinutes(-windowMinutes);

            int members = 0, guests = 0, bots = 0;
            var botNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in _visitors.Values)
            {
                if (entry.LastSeenUtc < since)
                {
                    continue;
                }

                switch (entry.Kind)
                {
                    case OnlineVisitorKind.Member:
                        members++;
                        break;
                    case OnlineVisitorKind.Guest:
                        guests++;
                        break;
                    case OnlineVisitorKind.Bot:
                        bots++;
                        var name = entry.BotName ?? "Other";
                        botNames[name] = botNames.TryGetValue(name, out var n) ? n + 1 : 1;
                        break;
                }
            }

            return new OnlineVisitorsSnapshot
            {
                WindowMinutes = windowMinutes,
                Members = members,
                Guests = guests,
                Bots = bots,
                BotNames = botNames
                    .OrderByDescending(b => b.Value)
                    .ThenBy(b => b.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        private void Record(string key, Entry entry, DateTime nowUtc, bool always)
        {
            PruneIfDue(nowUtc);

            if (!always && _visitors.Count >= MaxEntries && !_visitors.ContainsKey(key))
            {
                return;
            }

            _visitors[key] = entry;
        }

        private void PruneIfDue(DateTime nowUtc)
        {
            var last = Interlocked.Read(ref _lastPruneTicks);
            if (nowUtc.Ticks - last < PruneInterval.Ticks
                || Interlocked.CompareExchange(ref _lastPruneTicks, nowUtc.Ticks, last) != last)
            {
                return;
            }

            var cutoff = nowUtc - MaxWindow;
            foreach (var pair in _visitors)
            {
                if (pair.Value.LastSeenUtc < cutoff)
                {
                    _visitors.TryRemove(pair);
                }
            }
        }
    }
}
