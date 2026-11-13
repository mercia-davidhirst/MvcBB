using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.Shared.Interfaces
{
    public interface IForumThreadRepository
    {
        IReadOnlyList<ForumThread> GetAll();
        IReadOnlyList<ForumThread> GetByBoardId(int boardId);
        ForumThread? GetById(int id);
        ForumThread Add(ForumThread thread);
        void Update(ForumThread thread);
        void Remove(ForumThread thread);

        /// <summary>
        /// A single page of threads, already filtered to boards visible to the
        /// caller and ordered (sticky first, then most recently active) at the
        /// data layer - so listing threads doesn't mean loading every thread in
        /// the table into memory first. TotalCount is the full filtered count,
        /// for paging UI (not just the page returned).
        /// </summary>
        (IReadOnlyList<ForumThread> Items, int TotalCount) GetPage(int skip, int take, bool includePrivateBoards);

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
    }
}
