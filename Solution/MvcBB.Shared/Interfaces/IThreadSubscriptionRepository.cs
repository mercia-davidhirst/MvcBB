using MvcBB.Shared.Models.ThreadSubscription;

namespace MvcBB.Shared.Interfaces
{
    public interface IThreadSubscriptionRepository
    {
        ThreadSubscriptionModel? GetByThreadAndUser(int threadId, string userId);

        /// <summary>Everyone watching this thread - used to fan out notifications on a new reply.</summary>
        IReadOnlyList<ThreadSubscriptionModel> GetByThreadId(int threadId);

        ThreadSubscriptionModel Add(ThreadSubscriptionModel subscription);
        void Update(ThreadSubscriptionModel subscription);
        void Remove(int threadId, string userId);
    }
}
