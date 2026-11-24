namespace MvcBB.API.Services
{
    /// <summary>
    /// When the API sent each of its emails over the last 24 hours, so admin
    /// emails can keep under AdminEmail:MaxPerHour / MaxPerDay. Filled in by
    /// CountingEmailSender for every email the API sends. Kept in memory only,
    /// so the count starts again from zero when the API restarts.
    /// </summary>
    public class EmailSendCounter
    {
        /// <summary>How far back sends are kept - the longest limit's window.</summary>
        public static readonly TimeSpan Window = TimeSpan.FromHours(24);

        private readonly TimeProvider _timeProvider;
        private readonly Queue<DateTime> _sentAt = new();
        private readonly object _lock = new();

        public EmailSendCounter(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>Counts one email sent now.</summary>
        public void Record()
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            lock (_lock)
            {
                Prune(now);
                _sentAt.Enqueue(now);
            }
        }

        /// <summary>
        /// Adds sends that happened before the API started (admin email
        /// deliveries from the database), so a restart doesn't hand out the
        /// whole limit again. Other site emails sent before the restart aren't
        /// recorded anywhere and can't be added back.
        /// </summary>
        public void Seed(IEnumerable<DateTime> sentAtUtc)
        {
            lock (_lock)
            {
                var all = _sentAt.Concat(sentAtUtc).OrderBy(t => t).ToList();
                _sentAt.Clear();
                foreach (var sentAt in all)
                {
                    _sentAt.Enqueue(sentAt);
                }
                Prune(_timeProvider.GetUtcNow().UtcDateTime);
            }
        }

        /// <summary>How many emails were sent after sinceUtc.</summary>
        public int CountSince(DateTime sinceUtc)
        {
            lock (_lock)
            {
                Prune(_timeProvider.GetUtcNow().UtcDateTime);
                return _sentAt.Count(t => t > sinceUtc);
            }
        }

        /// <summary>When each email after sinceUtc was sent, oldest first.</summary>
        public IReadOnlyList<DateTime> SentSince(DateTime sinceUtc)
        {
            lock (_lock)
            {
                Prune(_timeProvider.GetUtcNow().UtcDateTime);
                return _sentAt.Where(t => t > sinceUtc).ToList();
            }
        }

        /// <summary>When the earliest email sent after sinceUtc went, or null if none were.</summary>
        public DateTime? OldestSince(DateTime sinceUtc)
        {
            lock (_lock)
            {
                Prune(_timeProvider.GetUtcNow().UtcDateTime);
                foreach (var sentAt in _sentAt)
                {
                    if (sentAt > sinceUtc)
                        return sentAt;
                }
                return null;
            }
        }

        // Sends are recorded in time order, so the old ones are at the front.
        private void Prune(DateTime nowUtc)
        {
            var cutoff = nowUtc - Window;
            while (_sentAt.Count > 0 && _sentAt.Peek() <= cutoff)
            {
                _sentAt.Dequeue();
            }
        }
    }
}
