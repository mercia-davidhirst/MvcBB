using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.API.InMemory
{
    /// <summary>
    /// In-memory email send log. Locks, since emails are sent (and logged) from
    /// requests and the background admin email sender at the same time.
    /// </summary>
    public class InMemoryEmailSendLogRepository : IEmailSendLogRepository
    {
        private readonly InMemoryStore _store;
        private readonly object _lock = new();

        public InMemoryEmailSendLogRepository(InMemoryStore store)
        {
            _store = store;
        }

        public void Add(DateTime sentAtUtc, EmailKind kind)
        {
            lock (_lock)
            {
                _store.EmailSendLog.Add((sentAtUtc == default ? DateTime.UtcNow : sentAtUtc, kind));
            }
        }

        // Same slots as usp_EmailSendLog_GetCounts / fn_email_send_log_get_counts.
        public IReadOnlyList<EmailSendCount> GetCounts(DateTime fromUtc, DateTime toUtc, int bucketMinutes)
        {
            lock (_lock)
            {
                return _store.EmailSendLog
                    .Where(e => e.SentAt >= fromUtc && e.SentAt < toUtc)
                    .GroupBy(e => (Bucket: fromUtc.AddMinutes((int)((e.SentAt - fromUtc).TotalMinutes / bucketMinutes) * bucketMinutes), e.Kind))
                    .Select(g => new EmailSendCount { BucketStart = g.Key.Bucket, Kind = g.Key.Kind, Count = g.Count() })
                    .OrderBy(c => c.BucketStart).ThenBy(c => c.Kind)
                    .ToList();
            }
        }

        public IReadOnlyList<DateTime> GetSentTimesSince(DateTime sinceUtc)
        {
            lock (_lock)
            {
                return _store.EmailSendLog
                    .Where(e => e.SentAt > sinceUtc)
                    .Select(e => e.SentAt)
                    .OrderBy(t => t)
                    .ToList();
            }
        }

        public int Prune(DateTime beforeUtc)
        {
            lock (_lock)
            {
                return _store.EmailSendLog.RemoveAll(e => e.SentAt < beforeUtc);
            }
        }
    }
}
