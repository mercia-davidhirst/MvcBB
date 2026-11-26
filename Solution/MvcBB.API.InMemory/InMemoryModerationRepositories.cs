using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Moderation;

namespace MvcBB.API.InMemory
{
    public class InMemoryModerationQueueRepository : IModerationQueueRepository
    {
        private readonly InMemoryStore _store;
        private readonly object _lock = new();

        // Items are removed once handled, so ids come from a counter rather
        // than Max + 1 (which would hand a handled item's id to the next one,
        // as an identity column never does).
        private int _lastId;

        public InMemoryModerationQueueRepository(InMemoryStore store)
        {
            _store = store;
        }

        public ModerationQueueItem Add(ModerationQueueItem item)
        {
            lock (_lock)
            {
                _lastId = Math.Max(_lastId, _store.ModerationQueue.Count == 0 ? 0 : _store.ModerationQueue.Max(q => q.Id)) + 1;
                item.Id = _lastId;
                item.CreatedAt = DateTime.UtcNow;
                _store.ModerationQueue.Add(item);
                return Hydrate(item);
            }
        }

        public ModerationQueueItem? GetById(int id)
        {
            lock (_lock)
            {
                var item = _store.ModerationQueue.FirstOrDefault(q => q.Id == id);
                return item == null ? null : Hydrate(item);
            }
        }

        public IReadOnlyList<ModerationQueueItem> GetAll()
        {
            lock (_lock)
            {
                return _store.ModerationQueue.OrderBy(q => q.CreatedAt).ThenBy(q => q.Id).Select(Hydrate).ToList();
            }
        }

        public void Remove(int id)
        {
            lock (_lock)
            {
                _store.ModerationQueue.RemoveAll(q => q.Id == id);
            }
        }

        public int Count()
        {
            lock (_lock)
            {
                return _store.ModerationQueue.Count;
            }
        }

        public int CountByUser(int userId)
        {
            lock (_lock)
            {
                return _store.ModerationQueue.Count(q => q.UserId == userId);
            }
        }

        private ModerationQueueItem Hydrate(ModerationQueueItem q) => new()
        {
            Id = q.Id,
            Kind = q.Kind,
            UserId = q.UserId,
            Username = _store.Users.FirstOrDefault(u => u.Id == q.UserId)?.Username ?? string.Empty,
            BoardId = q.BoardId,
            BoardName = _store.Boards.FirstOrDefault(b => b.Id == q.BoardId)?.Name,
            ThreadId = q.ThreadId,
            ThreadTitle = q.ThreadId.HasValue ? _store.Threads.FirstOrDefault(t => t.Id == q.ThreadId)?.Title : null,
            Title = q.Title,
            Content = q.Content,
            QuotedPostId = q.QuotedPostId,
            ImageUrl = q.ImageUrl,
            PollJson = q.PollJson,
            CreatedAt = q.CreatedAt
        };
    }

    public class InMemoryModerationLogRepository : IModerationLogRepository
    {
        private readonly InMemoryStore _store;
        private readonly object _lock = new();

        public InMemoryModerationLogRepository(InMemoryStore store)
        {
            _store = store;
        }

        public void Add(ModerationLogEntry entry)
        {
            lock (_lock)
            {
                entry.Id = _store.ModerationLog.Count == 0 ? 1 : _store.ModerationLog.Max(l => l.Id) + 1;
                entry.CreatedAt = DateTime.UtcNow;
                _store.ModerationLog.Add(entry);
            }
        }

        public (IReadOnlyList<ModerationLogEntry> Items, int TotalCount) GetPage(int skip, int take)
        {
            lock (_lock)
            {
                var items = _store.ModerationLog
                    .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
                    .Skip(Math.Max(0, skip)).Take(Math.Max(1, take))
                    .Select(l => new ModerationLogEntry
                    {
                        Id = l.Id,
                        ActorUserId = l.ActorUserId,
                        ActorUsername = _store.Users.FirstOrDefault(u => u.Id == l.ActorUserId)?.Username,
                        Action = l.Action,
                        TargetType = l.TargetType,
                        TargetId = l.TargetId,
                        TargetUserId = l.TargetUserId,
                        TargetUsername = l.TargetUserId.HasValue ? _store.Users.FirstOrDefault(u => u.Id == l.TargetUserId)?.Username : null,
                        Summary = l.Summary,
                        Reason = l.Reason,
                        CreatedAt = l.CreatedAt
                    })
                    .ToList();
                return (items, _store.ModerationLog.Count);
            }
        }
    }

    public class InMemoryModerationSettingsRepository : IModerationSettingsRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryModerationSettingsRepository(InMemoryStore store)
        {
            _store = store;
        }

        public ModerationSettings Get() => new()
        {
            HoldNewMemberPosts = _store.ModerationSettings.HoldNewMemberPosts,
            NewMemberPostThreshold = _store.ModerationSettings.NewMemberPostThreshold
        };

        public void Update(ModerationSettings settings) => _store.ModerationSettings = new ModerationSettings
        {
            HoldNewMemberPosts = settings.HoldNewMemberPosts,
            NewMemberPostThreshold = settings.NewMemberPostThreshold
        };
    }
}
