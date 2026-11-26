namespace MvcBB.App.Models
{
    /// <summary>
    /// Names for the post-editor drafts that wwwroot/js/post-editor.js keeps
    /// in the browser's localStorage while a member types (one per editor:
    /// a new discussion in a board, a reply in a thread, an edit of a post).
    /// The view puts the key on the textarea (data-draft-key); once the post
    /// has been saved, the controller puts it in TempData[ClearDraftTempDataKey]
    /// and _Layout tells the script to delete that draft. A post that fails
    /// (an expired session, an error) never reaches that point, so its draft
    /// is still there to restore.
    /// </summary>
    public static class DraftKeys
    {
        public const string ClearDraftTempDataKey = "ClearDraft";

        public static string NewThread(int boardId) => $"thread-new-{boardId}";
        public static string Reply(int threadId) => $"reply-{threadId}";
        public static string Edit(int postId) => $"edit-{postId}";
    }
}
