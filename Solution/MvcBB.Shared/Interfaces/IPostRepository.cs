using MvcBB.Shared.Models.Post;

namespace MvcBB.Shared.Interfaces
{
    public interface IPostRepository
    {
        IReadOnlyList<Post> GetAll();
        IReadOnlyList<Post> GetByThreadId(int threadId);

        /// <summary>
        /// One page of a discussion's posts, in order (CreatedAt, then Id),
        /// paged in the data layer - a discussion can have tens of thousands
        /// of posts, so GetByThreadId is never used to show one.
        /// </summary>
        IReadOnlyList<Post> GetThreadPage(int threadId, int skip, int take);

        /// <summary>
        /// How many posts the discussion has, plus where one post sits in it:
        /// postId when given (null result when it isn't in this thread), or
        /// else the first post created after unreadAfter that isn't by
        /// excludeUserId (the reader's first unread post). Used to open a
        /// discussion at the page that post is on.
        /// </summary>
        ThreadPostLocation GetThreadLocation(int threadId, int? postId = null, DateTime? unreadAfter = null, int? excludeUserId = null);

        Post? GetById(int id);
        Post Add(Post post);
        void Update(Post post);

        /// <summary>
        /// Whether this user has (or had) at least one post in this thread -
        /// used to decide whether a new post makes them a new distinct
        /// participant, without loading/counting every post in the thread.
        /// </summary>
        bool HasPostInThread(int threadId, string userId);

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
        /// recent ones. Used by a member's profile page. Posts from private
        /// boards are only included when includePrivateBoards is true (a
        /// signed-in caller), and posts from inactive boards only when
        /// includeInactiveBoards is true (an administrator).
        /// </summary>
        IReadOnlyList<Post> GetByUserId(int userId, int take, bool includePrivateBoards, bool includeInactiveBoards);
    }
}
