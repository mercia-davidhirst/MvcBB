using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ThreadSubscription;

namespace MvcBB.API.InMemory
{
    public class InMemoryThreadSubscriptionRepository : IThreadSubscriptionRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryThreadSubscriptionRepository(InMemoryStore store)
        {
            _store = store;
        }

        public ThreadSubscriptionModel? GetByThreadAndUser(int threadId, string userId) =>
            _store.ThreadSubscriptions.FirstOrDefault(s => s.ThreadId == threadId && s.UserId == userId);

        public IReadOnlyList<ThreadSubscriptionModel> GetByThreadId(int threadId) =>
            _store.ThreadSubscriptions.Where(s => s.ThreadId == threadId).ToList();

        public ThreadSubscriptionModel Add(ThreadSubscriptionModel subscription)
        {
            if (subscription.Id == 0)
                subscription.Id = _store.ThreadSubscriptions.Count == 0 ? 1 : _store.ThreadSubscriptions.Max(s => s.Id) + 1;
            _store.ThreadSubscriptions.Add(subscription);
            return subscription;
        }

        public void Update(ThreadSubscriptionModel subscription)
        {
            var index = _store.ThreadSubscriptions.FindIndex(s => s.Id == subscription.Id);
            if (index >= 0)
                _store.ThreadSubscriptions[index] = subscription;
        }

        public void Remove(int threadId, string userId)
        {
            var existing = GetByThreadAndUser(threadId, userId);
            if (existing != null)
                _store.ThreadSubscriptions.Remove(existing);
        }
    }
}
