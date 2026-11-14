using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.Shared.Interfaces
{
    public interface IForumThreadRepository
    {
        IReadOnlyList<ForumThread> GetAll();
        ForumThread? GetById(int id);
        ForumThread Add(ForumThread thread);
        void Update(ForumThread thread);
        void Remove(ForumThread thread);

        /// <summary>
        /// A single page of threads, already filtered to boards visible to the
        /// caller (and, when boardId is given, to just that one board - e.g. a
        /// board's own thread listing) and ordered (sticky first, then most
        /// recently active) at the data layer - so listing threads doesn't mean
        /// loading every thread in the table into memory first. TotalCount is
        /// the full filtered count, for paging UI (not just the page returned).
        /// </summary>
        (IReadOnlyList<ForumThread> Items, int TotalCount) GetPage(int skip, int take, bool includePrivateBoards, int? boardId = null);

        /// <summary>
        /// For each of the given board ids: the thread most recently active in
        /// that board (by LastPostAt, falling back to CreatedAt) - computed in
        /// one query across every board at once, rather than fetching every
        /// thread in a board just to find the newest one, once per board.
        /// Boards.LastPostAt/LastPostByUserId can't be used for this directly -
        /// they're only set when a thread is first created, not when someone
        /// replies to an existing one, so they go stale.
        /// </summary>
        IReadOnlyDictionary<int, ForumThread> GetLastActiveThreadsByBoardIds(IEnumerable<int> boardIds);

        /// <summary>Multiple threads by id in one query - e.g. batch-resolving each search result's board.</summary>
        IReadOnlyDictionary<int, ForumThread> GetByIds(IEnumerable<int> ids);

        /// <summary>
        /// Threads whose title or author username contains the query (case-
        /// insensitive), optionally restricted to one board, filtered to
        /// boards visible to the caller, newest-first and paginated entirely
        /// in the data layer - so search doesn't mean loading every thread
        /// in the table first. TotalCount is the full filtered count, not
        /// just the page returned.
        /// </summary>
        (IReadOnlyList<ForumThread> Items, int TotalCount) Search(string query, int? boardId, bool includePrivateBoards, int skip, int take);
    }
}
