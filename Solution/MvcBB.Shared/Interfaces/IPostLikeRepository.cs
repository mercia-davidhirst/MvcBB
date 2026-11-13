namespace MvcBB.Shared.Interfaces
{
    public interface IPostLikeRepository
    {
        bool HasLiked(int postId, string userId);
        int GetLikeCount(int postId);

        /// <summary>
        /// Like counts for several posts at once (e.g. a whole thread's worth),
        /// keyed by PostId - avoids one query per post when mapping a list.
        /// </summary>
        IReadOnlyDictionary<int, int> GetLikeCounts(IEnumerable<int> postIds);

        /// <summary>Which of these post ids this user has liked, in one query.</summary>
        ISet<int> GetLikedPostIds(IEnumerable<int> postIds, string userId);

        void AddLike(int postId, string userId);
        void RemoveLike(int postId, string userId);
    }
}
