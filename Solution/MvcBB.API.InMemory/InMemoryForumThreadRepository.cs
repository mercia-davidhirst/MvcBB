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

        public IReadOnlyList<ForumThread> GetAll() => _store.Threads.ToList();

        public IReadOnlyList<ForumThread> GetByBoardId(int boardId) =>
            _store.Threads
                .Where(t => t.BoardId == boardId)
                .OrderByDescending(t => t.IsSticky)
                .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt)
                .ToList();

        public ForumThread? GetById(int id) => _store.Threads.FirstOrDefault(t => t.Id == id);

        public ForumThread Add(ForumThread thread)
        {
            if (thread.Id == 0)
                thread.Id = _store.Threads.Count == 0 ? 1 : _store.Threads.Max(t => t.Id) + 1;
            _store.Threads.Add(thread);
            return thread;
        }

        public void Update(ForumThread thread)
        {
            var index = _store.Threads.FindIndex(t => t.Id == thread.Id);
            if (index >= 0)
                _store.Threads[index] = thread;
        }

        public void Remove(ForumThread thread) => _store.Threads.Remove(thread);
    }
}
