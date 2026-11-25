using MvcBB.Shared.Models.ThreadRead;

namespace MvcBB.Shared.Interfaces
{
    public interface IThreadReadRepository
    {
        ThreadReadModel? GetByThreadAndUser(int threadId, string userId);

        /// <summary>
        /// Records that the member has read up to the given post. Creates
        /// their row on the first call and afterwards only moves it forward:
        /// a post created at or before the saved position leaves it alone, so
        /// scrolling back up never makes newer posts unread again.
        /// </summary>
        void MarkRead(int threadId, string userId, int postId, DateTime postCreatedAt);
    }
}
