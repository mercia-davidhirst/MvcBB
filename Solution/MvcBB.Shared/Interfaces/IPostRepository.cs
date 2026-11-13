using MvcBB.Shared.Models.Post;

namespace MvcBB.Shared.Interfaces
{
    public interface IPostRepository
    {
        IReadOnlyList<Post> GetAll();
        IReadOnlyList<Post> GetByThreadId(int threadId);
        Post? GetById(int id);
        Post Add(Post post);
        void Update(Post post);

        /// <summary>
        /// For each of the given thread ids: how many distinct users posted,
        /// and every post id in that thread - one query instead of one
        /// GetByThreadId call per thread. A thread with no posts is simply
        /// absent from the result.
        /// </summary>
        IReadOnlyDictionary<int, ThreadPostSummary> GetThreadSummaries(IEnumerable<int> threadIds);

        /// <summary>Multiple posts by id in one query - e.g. batch-resolving quoted posts on a page.</summary>
        IReadOnlyDictionary<int, Post> GetByIds(IEnumerable<int> ids);

        /// <summary>
        /// Posts whose content or author username contains the query (case-
        /// insensitive), optionally restricted to one board (via the post's
        /// thread), filtered to boards visible to the caller, newest-first
        /// and paginated entirely in the data layer. TotalCount is the full
        /// filtered count, not just the page returned.
        /// </summary>
        (IReadOnlyList<Post> Items, int TotalCount) Search(string query, int? boardId, bool includePrivateBoards, int skip, int take);

        /// <summary>
        /// A user's most recent non-deleted posts (newest first, capped at
        /// take) - filtered, sorted and limited in the data layer instead of
        /// loading every post in the table to find one user's handful of
        /// recent ones. Used by a member's profile page.
        /// </summary>
        IReadOnlyList<Post> GetByUserId(int userId, int take);
    }
}
