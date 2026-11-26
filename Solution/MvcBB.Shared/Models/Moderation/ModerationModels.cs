using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Moderation
{
    /// <summary>What a held post would become once approved. Stored as a number, so never renumber these.</summary>
    public enum QueuedPostKind
    {
        /// <summary>A new discussion (its title, first post and optional poll).</summary>
        NewThread = 1,

        /// <summary>A reply in an existing discussion.</summary>
        Reply = 2
    }

    /// <summary>
    /// A post held for a moderator's approval (see ModerationSettings). It
    /// isn't a real thread or post yet, so nobody else can see it; approving
    /// it creates the thread or reply exactly as if it had just been posted,
    /// and turning it down deletes it.
    /// </summary>
    public class ModerationQueueItem
    {
        public int Id { get; set; }
        public QueuedPostKind Kind { get; set; }

        /// <summary>The member who wrote it.</summary>
        public int UserId { get; set; }

        /// <summary>Filled in on read, from Users.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>The board it was posted in (for a reply, the board of its thread).</summary>
        public int BoardId { get; set; }

        /// <summary>Filled in on read, from Boards.</summary>
        public string? BoardName { get; set; }

        /// <summary>The thread a reply belongs to; null for a new discussion.</summary>
        public int? ThreadId { get; set; }

        /// <summary>Filled in on read, from Threads.</summary>
        public string? ThreadTitle { get; set; }

        /// <summary>A new discussion's title; null for a reply.</summary>
        public string? Title { get; set; }

        public string Content { get; set; } = string.Empty;
        public int? QuotedPostId { get; set; }
        public string? ImageUrl { get; set; }

        /// <summary>A new discussion's poll (a serialized CreatePollRequest), or null.</summary>
        public string? PollJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// When new members' posts are held for approval. A single row, edited
    /// by administrators on the Moderation settings page.
    /// </summary>
    public class ModerationSettings
    {
        /// <summary>Hold posts from new members until a moderator approves them. Off by default.</summary>
        [Display(Name = "Hold new members' posts for approval")]
        public bool HoldNewMemberPosts { get; set; }

        /// <summary>
        /// A member counts as new until this many of their posts are live
        /// (approved, not deleted). 1 holds just their first post.
        /// </summary>
        [Range(1, 100, ErrorMessage = "Enter a number from 1 to 100")]
        [Display(Name = "Posts to hold")]
        public int NewMemberPostThreshold { get; set; } = 1;
    }

    /// <summary>
    /// One moderator action in the moderation log: who did what to which
    /// post, thread, member or report, and when. Written by the API as the
    /// action happens; never edited or deleted from the site.
    /// </summary>
    public class ModerationLogEntry
    {
        public int Id { get; set; }

        /// <summary>The moderator or administrator who did it.</summary>
        public int ActorUserId { get; set; }

        /// <summary>Filled in on read, from Users.</summary>
        public string? ActorUsername { get; set; }

        /// <summary>One of ModerationActions.</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>"Post", "Thread", "User", "Report" or "Queue".</summary>
        public string TargetType { get; set; } = string.Empty;

        public int? TargetId { get; set; }

        /// <summary>The member the action concerned (a post's author, a locked member...), if any.</summary>
        public int? TargetUserId { get; set; }

        /// <summary>Filled in on read, from Users.</summary>
        public string? TargetUsername { get; set; }

        /// <summary>What happened, in a sentence (e.g. the thread's title), written at the time.</summary>
        public string Summary { get; set; } = string.Empty;

        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>A page of the moderation log, newest first.</summary>
    public class ModerationLogPage
    {
        public List<ModerationLogEntry> Entries { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>The action names written to ModerationLogEntry.Action.</summary>
    public static class ModerationActions
    {
        public const string ApprovePost = "post.approve";
        public const string RejectPost = "post.reject";
        public const string DeletePost = "post.delete";
        public const string EditPost = "post.edit";
        public const string DeleteThread = "thread.delete";
        public const string LockThread = "thread.lock";
        public const string UnlockThread = "thread.unlock";
        public const string PinThread = "thread.pin";
        public const string UnpinThread = "thread.unpin";
        public const string RenameThread = "thread.rename";
        public const string LockUser = "user.lock";
        public const string UnlockUser = "user.unlock";
        public const string ChangeRole = "user.role";
        public const string SetCustomTitle = "user.title";
        public const string AnonymizeUser = "user.anonymize";
        public const string UpdateReport = "report.update";
        public const string ChangeSettings = "settings.moderation";

        private static readonly Dictionary<string, string> DisplayNames = new()
        {
            [ApprovePost] = "Approved held post",
            [RejectPost] = "Turned down held post",
            [DeletePost] = "Deleted post",
            [EditPost] = "Edited member's post",
            [DeleteThread] = "Deleted thread",
            [LockThread] = "Locked thread",
            [UnlockThread] = "Unlocked thread",
            [PinThread] = "Pinned thread",
            [UnpinThread] = "Unpinned thread",
            [RenameThread] = "Renamed thread",
            [LockUser] = "Locked member",
            [UnlockUser] = "Unlocked member",
            [ChangeRole] = "Changed member's role",
            [SetCustomTitle] = "Set custom title",
            [AnonymizeUser] = "Anonymized member",
            [UpdateReport] = "Updated report",
            [ChangeSettings] = "Changed moderation settings"
        };

        public static string DisplayName(string action) =>
            DisplayNames.TryGetValue(action, out var name) ? name : action;

        public static IReadOnlyDictionary<string, string> All => DisplayNames;
    }

    /// <summary>Body of POST api/moderation/queue/{id}/reject.</summary>
    public class RejectQueuedPostRequest
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>What the API returns when it holds a new thread or reply for approval (HTTP 202).</summary>
    public class HeldForApprovalResponse
    {
        public bool Held { get; set; } = true;
        public int QueueItemId { get; set; }
        public string Message { get; set; } = "Thanks! Your post will appear once a moderator has approved it.";
    }

    /// <summary>What approving a held post created.</summary>
    public class ApproveQueuedPostResponse
    {
        public int ThreadId { get; set; }
        public int PostId { get; set; }
    }
}
