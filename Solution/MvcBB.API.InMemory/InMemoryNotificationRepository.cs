using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Notification;

namespace MvcBB.API.InMemory
{
    public class InMemoryNotificationRepository : INotificationRepository
    {
        private readonly InMemoryStore _store;
        private readonly object _lock = new();

        public InMemoryNotificationRepository(InMemoryStore store)
        {
            _store = store;
        }

        public Notification Add(Notification notification)
        {
            lock (_lock)
            {
                notification.Id = _store.Notifications.Count == 0 ? 1 : _store.Notifications.Max(n => n.Id) + 1;
                notification.CreatedAt = DateTime.UtcNow;
                _store.Notifications.Add(Copy(notification));
                return notification;
            }
        }

        public Notification? GetById(int id)
        {
            lock (_lock)
            {
                var found = _store.Notifications.FirstOrDefault(n => n.Id == id);
                return found == null ? null : Hydrate(found);
            }
        }

        public (IReadOnlyList<Notification> Items, int TotalCount) GetByUser(int userId, int skip, int take)
        {
            lock (_lock)
            {
                var mine = _store.Notifications.Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                    .ToList();
                return (mine.Skip(Math.Max(0, skip)).Take(Math.Max(1, take)).Select(Hydrate).ToList(), mine.Count);
            }
        }

        public int CountUnread(int userId)
        {
            lock (_lock)
            {
                return _store.Notifications.Count(n => n.UserId == userId && !n.IsRead);
            }
        }

        public bool Exists(int userId, NotificationType type, int postId, int? actorUserId = null)
        {
            lock (_lock)
            {
                return _store.Notifications.Any(n => n.UserId == userId && n.Type == type && n.PostId == postId
                    && (actorUserId == null || n.ActorUserId == actorUserId));
            }
        }

        public void MarkRead(int id, int userId) => MarkWhere(n => n.UserId == userId && n.Id == id);
        public void MarkAllRead(int userId) => MarkWhere(n => n.UserId == userId);
        public void MarkReadForThread(int userId, int threadId) => MarkWhere(n => n.UserId == userId && n.ThreadId == threadId);
        public void MarkReadForMessage(int userId, int messageId) => MarkWhere(n => n.UserId == userId && n.MessageId == messageId);

        private void MarkWhere(Func<Notification, bool> predicate)
        {
            lock (_lock)
            {
                foreach (var n in _store.Notifications.Where(predicate))
                {
                    n.IsRead = true;
                }
            }
        }

        public int PruneRead(DateTime createdBefore)
        {
            lock (_lock)
            {
                return _store.Notifications.RemoveAll(n => n.IsRead && n.CreatedAt < createdBefore);
            }
        }

        private Notification Hydrate(Notification n)
        {
            var copy = Copy(n);
            copy.ActorUsername = n.ActorUserId.HasValue ? _store.Users.FirstOrDefault(u => u.Id == n.ActorUserId)?.Username : null;
            copy.ThreadTitle = n.ThreadId.HasValue ? _store.Threads.FirstOrDefault(t => t.Id == n.ThreadId)?.Title : null;
            return copy;
        }

        private static Notification Copy(Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Type = n.Type,
            ActorUserId = n.ActorUserId,
            ThreadId = n.ThreadId,
            PostId = n.PostId,
            MessageId = n.MessageId,
            Excerpt = n.Excerpt,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}
