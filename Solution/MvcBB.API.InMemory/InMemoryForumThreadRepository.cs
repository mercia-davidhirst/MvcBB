using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.InMemory
{
    public class InMemoryForumThreadRepository : IForumThreadRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryForumThreadRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<ForumThread> GetAll() => _store.Threads.Select(Hydrate).ToList();

        public ForumThread? GetById(int id)
        {
            var thread = _store.Threads.FirstOrDefault(t => t.Id == id);
            return thread == null ? null : Hydrate(thread);
        }

        public ForumThread Add(ForumThread thread)
        {
            if (thread.Id == 0)
                thread.Id = _store.Threads.Count == 0 ? 1 : _store.Threads.Max(t => t.Id) + 1;
            _store.Threads.Add(thread);
            return Hydrate(thread);
        }

        public void Update(ForumThread thread)
        {
            var index = _store.Threads.FindIndex(t => t.Id == thread.Id);
            if (index >= 0)
                _store.Threads[index] = thread;
        }

        public void Remove(ForumThread thread) => _store.Threads.Remove(thread);

        public (IReadOnlyList<ForumThread> Items, int TotalCount) GetPage(int skip, int take, bool includePrivateBoards, int? boardId = null)
        {
            var visibleBoardIds = _store.Boards
                .Where(b => includePrivateBoards || !b.IsPrivate)
                .Select(b => b.Id)
                .ToHashSet();

            var filtered = _store.Threads
                .Where(t => visibleBoardIds.Contains(t.BoardId))
                .Where(t => !boardId.HasValue || t.BoardId == boardId.Value)
                .OrderByDescending(t => t.IsSticky)
                .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt)
                .ToList();

            var page = filtered.Skip(skip).Take(take).Select(Hydrate).ToList();
            return (page, filtered.Count);
        }

        public IReadOnlyDictionary<int, ForumThread> GetLastActiveThreadsByBoardIds(IEnumerable<int> boardIds)
        {
            var idSet = boardIds.ToHashSet();
            return _store.Threads
                .Where(t => idSet.Contains(t.BoardId))
                .GroupBy(t => t.BoardId)
                .ToDictionary(
                    g => g.Key,
                    g => Hydrate(g.OrderByDescending(t => t.LastPostAt ?? t.CreatedAt).First()));
        }

        public IReadOnlyDictionary<int, ForumThread> GetByIds(IEnumerable<int> ids)
        {
            var idSet = ids.ToHashSet();
            return _store.Threads.Where(t => idSet.Contains(t.Id)).Select(Hydrate).ToDictionary(t => t.Id);
        }

        public (IReadOnlyList<ForumThread> Items, int TotalCount) Search(string query, int? boardId, bool includePrivateBoards, int skip, int take)
        {
            var visibleBoardIds = _store.Boards
                .Where(b => includePrivateBoards || !b.IsPrivate)
                .Select(b => b.Id)
                .ToHashSet();

            var q = query.ToLowerInvariant();
            var filtered = _store.Threads
                .Where(t => visibleBoardIds.Contains(t.BoardId))
                .Where(t => !boardId.HasValue || t.BoardId == boardId.Value)
                .Select(Hydrate)
                .Where(t => t.Title.ToLowerInvariant().Contains(q) || t.CreatedByUsername.ToLowerInvariant().Contains(q))
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return (filtered.Skip(skip).Take(take).ToList(), filtered.Count);
        }

        // Populates the denormalized presentation fields (BoardName,
        // BoardIsPrivate, CreatedByUsername, LastPostByUsername) from the
        // current Board/User records, matching what the SQL/PostgreSQL
        // repositories get for free from vw_ThreadDetails/vw_thread_details.
        private ForumThread Hydrate(ForumThread thread)
        {
            var board = _store.Boards.FirstOrDefault(b => b.Id == thread.BoardId);
            thread.BoardName = board?.Name ?? string.Empty;
            thread.BoardIsPrivate = board?.IsPrivate ?? false;

            if (int.TryParse(thread.CreatedByUserId, out var creatorId))
            {
                thread.CreatedByUsername = _store.Users.FirstOrDefault(u => u.Id == creatorId)?.Username ?? string.Empty;
            }

            if (int.TryParse(thread.LastPostByUserId, out var lastPosterId))
            {
                thread.LastPostByUsername = _store.Users.FirstOrDefault(u => u.Id == lastPosterId)?.Username;
            }
            else
            {
                thread.LastPostByUsername = null;
            }

            return thread;
        }
    }
}
