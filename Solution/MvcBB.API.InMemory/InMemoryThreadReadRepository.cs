using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ThreadRead;

namespace MvcBB.API.InMemory
{
    public class InMemoryThreadReadRepository : IThreadReadRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryThreadReadRepository(InMemoryStore store)
        {
            _store = store;
        }

        public ThreadReadModel? GetByThreadAndUser(int threadId, string userId)
        {
            lock (_store.ThreadReads)
            {
                return _store.ThreadReads.FirstOrDefault(r => r.ThreadId == threadId && r.UserId == userId);
            }
        }

        public void MarkRead(int threadId, string userId, int postId, DateTime postCreatedAt)
        {
            lock (_store.ThreadReads)
            {
                var existing = _store.ThreadReads.FirstOrDefault(r => r.ThreadId == threadId && r.UserId == userId);
                if (existing == null)
                {
                    _store.ThreadReads.Add(new ThreadReadModel
                    {
                        ThreadId = threadId,
                        UserId = userId,
                        LastReadPostId = postId,
                        LastReadPostAt = postCreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else if (existing.LastReadPostAt < postCreatedAt)
                {
                    existing.LastReadPostId = postId;
                    existing.LastReadPostAt = postCreatedAt;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
