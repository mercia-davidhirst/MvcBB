namespace MvcBB.Shared.Models.Notification
{
    /// <summary>What a notification is about. Stored as a number, so never renumber these.</summary>
    public enum NotificationType
    {
        /// <summary>Someone replied in a discussion the member started or is watching.</summary>
        Reply = 1,

        /// <summary>Someone @mentioned the member in a post.</summary>
        Mention = 2,

        /// <summary>Someone liked one of the member's posts.</summary>
        Like = 3,

        /// <summary>Someone sent the member a private message.</summary>
        Message = 4,

        /// <summary>Someone quoted one of the member's posts.</summary>
        Quote = 5,

        /// <summary>A moderator approved or turned down a post the member made while it was held for approval.</summary>
        Moderation = 6
    }

    /// <summary>
    /// One entry in a member's notification centre. Created by the API when
    /// something happens that involves the member (see
    /// MvcBB.API.Services.NotificationService), never by the member.
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }

        /// <summary>The member being notified.</summary>
        public int UserId { get; set; }

        public NotificationType Type { get; set; }

        /// <summary>The member who did it (replied, liked, ...), or null for the forum itself.</summary>
        public int? ActorUserId { get; set; }

        /// <summary>Filled in on read, from Users.</summary>
        public string? ActorUsername { get; set; }

        public int? ThreadId { get; set; }

        /// <summary>Filled in on read, from Threads (null once the thread is gone).</summary>
        public string? ThreadTitle { get; set; }

        public int? PostId { get; set; }
        public int? MessageId { get; set; }

        /// <summary>A short plain-text extract (a message subject, the start of a post, or a moderator's note).</summary>
        public string? Excerpt { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>A page of a member's notifications, newest first.</summary>
    public class NotificationPage
    {
        public List<Notification> Notifications { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
