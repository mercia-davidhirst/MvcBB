using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.InMemory
{
    /// <summary>
    /// In-memory admin email repository. Unlike most InMemory repositories this
    /// one locks, because the background sender (AdminEmailDispatcher) updates
    /// progress on its own thread while requests read the log.
    /// </summary>
    public class InMemoryAdminEmailRepository : IAdminEmailRepository
    {
        private readonly InMemoryStore _store;
        private readonly object _lock = new();

        public InMemoryAdminEmailRepository(InMemoryStore store)
        {
            _store = store;
        }

        public AdminEmail Add(AdminEmail email)
        {
            lock (_lock)
            {
                email.Id = _store.AdminEmails.Count == 0 ? 1 : _store.AdminEmails.Max(e => e.Id) + 1;
                if (email.CreatedAt == default)
                    email.CreatedAt = DateTime.UtcNow;
                _store.AdminEmails.Add(Copy(email));
                return Copy(email);
            }
        }

        public void UpdateProgress(AdminEmail email)
        {
            lock (_lock)
            {
                var stored = _store.AdminEmails.FirstOrDefault(e => e.Id == email.Id);
                if (stored == null)
                    return;
                stored.Status = email.Status;
                stored.RecipientCount = email.RecipientCount;
                stored.SentCount = email.SentCount;
                stored.FailedCount = email.FailedCount;
                stored.CompletedAt = email.CompletedAt;
                stored.PausedUntil = email.PausedUntil;
            }
        }

        public AdminEmail? GetById(int id)
        {
            lock (_lock)
            {
                var stored = _store.AdminEmails.FirstOrDefault(e => e.Id == id);
                return stored == null ? null : Copy(stored);
            }
        }

        public IReadOnlyList<AdminEmail> GetRecent(int take)
        {
            lock (_lock)
            {
                return _store.AdminEmails
                    .OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
                    .Take(take)
                    .Select(Copy)
                    .ToList();
            }
        }

        public int MarkUnfinishedInterrupted()
        {
            lock (_lock)
            {
                var unfinished = _store.AdminEmails
                    .Where(e => e.Status is AdminEmailStatus.Queued or AdminEmailStatus.Sending)
                    .ToList();
                foreach (var email in unfinished)
                {
                    email.Status = AdminEmailStatus.Interrupted;
                    email.CompletedAt = DateTime.UtcNow;
                    email.PausedUntil = null;
                }
                return unfinished.Count;
            }
        }

        public void AddFailure(AdminEmailFailure failure)
        {
            lock (_lock)
            {
                var stored = Copy(failure);
                stored.Id = _store.AdminEmailFailures.Count == 0 ? 1 : _store.AdminEmailFailures.Max(f => f.Id) + 1;
                if (stored.FailedAt == default)
                    stored.FailedAt = DateTime.UtcNow;
                _store.AdminEmailFailures.Add(stored);
            }
        }

        public IReadOnlyList<AdminEmailFailure> GetFailures(int adminEmailId)
        {
            lock (_lock)
            {
                return _store.AdminEmailFailures
                    .Where(f => f.AdminEmailId == adminEmailId)
                    .OrderBy(f => f.FailedAt).ThenBy(f => f.Id)
                    .Select(Copy)
                    .ToList();
            }
        }

        public void AddDelivery(int adminEmailId, int userId, DateTime sentAt)
        {
            lock (_lock)
            {
                if (!_store.AdminEmailDeliveries.Any(d => d.AdminEmailId == adminEmailId && d.UserId == userId))
                    _store.AdminEmailDeliveries.Add((adminEmailId, userId, sentAt == default ? DateTime.UtcNow : sentAt));
            }
        }

        public IReadOnlyList<int> GetDeliveredUserIds(int adminEmailId)
        {
            lock (_lock)
            {
                return _store.AdminEmailDeliveries
                    .Where(d => d.AdminEmailId == adminEmailId)
                    .Select(d => d.UserId)
                    .ToList();
            }
        }

        public bool IsOptedOut(int userId)
        {
            lock (_lock)
            {
                return _store.AdminEmailOptOuts.Contains(userId);
            }
        }

        public void SetOptedOut(int userId, bool optedOut)
        {
            lock (_lock)
            {
                if (optedOut)
                    _store.AdminEmailOptOuts.Add(userId);
                else
                    _store.AdminEmailOptOuts.Remove(userId);
            }
        }

        public IReadOnlyList<AdminEmailRecipient> GetRecipients(UserRole? role, int? userId, bool includeOptedOut)
        {
            lock (_lock)
            {
                return Audience(role, userId)
                    .Where(u => includeOptedOut || !_store.AdminEmailOptOuts.Contains(u.Id))
                    .OrderBy(u => u.Id)
                    .Select(u => new AdminEmailRecipient { UserId = u.Id, Username = u.Username, Email = u.Email })
                    .ToList();
            }
        }

        public (int Total, int OptedOut) CountRecipients(UserRole? role, int? userId)
        {
            lock (_lock)
            {
                var audience = Audience(role, userId).ToList();
                return (audience.Count, audience.Count(u => _store.AdminEmailOptOuts.Contains(u.Id)));
            }
        }

        // Same filter as usp_AdminEmail_GetRecipients / fn_admin_email_get_recipients.
        // ToList() first: the user list belongs to InMemoryUserRepository, which
        // doesn't share this lock.
        private IEnumerable<User> Audience(UserRole? role, int? userId) =>
            _store.Users.ToList().Where(u =>
                u.Role != UserRole.Deleted &&
                !string.IsNullOrEmpty(u.Email) &&
                (role == null || u.Role == role) &&
                (userId == null || u.Id == userId));

        // Callers get copies so a progress update can't race a page reading the same object.
        private static AdminEmail Copy(AdminEmail e) => new()
        {
            Id = e.Id,
            SentByUserId = e.SentByUserId,
            SentByUsername = e.SentByUsername,
            CreatedAt = e.CreatedAt,
            CompletedAt = e.CompletedAt,
            Subject = e.Subject,
            Body = e.Body,
            Audience = e.Audience,
            Role = e.Role,
            UserId = e.UserId,
            SenderMode = e.SenderMode,
            FromAddress = e.FromAddress,
            FromName = e.FromName,
            ReplyToAddress = e.ReplyToAddress,
            IsImportant = e.IsImportant,
            Status = e.Status,
            RecipientCount = e.RecipientCount,
            SentCount = e.SentCount,
            FailedCount = e.FailedCount,
            PausedUntil = e.PausedUntil
        };

        private static AdminEmailFailure Copy(AdminEmailFailure f) => new()
        {
            Id = f.Id,
            AdminEmailId = f.AdminEmailId,
            UserId = f.UserId,
            Username = f.Username,
            Email = f.Email,
            Reason = f.Reason,
            FailedAt = f.FailedAt
        };
    }
}
