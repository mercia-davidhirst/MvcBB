namespace MvcBB.Shared.Models.Board
{
    public static class BoardVisibilityExtensions
    {
        /// <summary>
        /// A private board (and its threads/posts) is only visible to signed-in
        /// callers; a non-private board is visible to everyone.
        /// </summary>
        public static bool IsVisibleTo(this Board board, bool isAuthenticated) => !board.IsPrivate || isAuthenticated;
    }
}
