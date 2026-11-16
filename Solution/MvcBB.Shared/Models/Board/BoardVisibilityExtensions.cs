namespace MvcBB.Shared.Models.Board
{
    public static class BoardVisibilityExtensions
    {
        /// <summary>
        /// A private board (and its threads/posts) is only visible to signed-in
        /// callers; a non-private board is visible to everyone. An inactive
        /// board is only visible to administrators, regardless of privacy.
        /// </summary>
        public static bool IsVisibleTo(this Board board, bool isAuthenticated, bool isAdmin) =>
            (isAdmin || board.IsActive) && (!board.IsPrivate || isAuthenticated);
    }
}
