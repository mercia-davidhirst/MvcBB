namespace MvcBB.Shared.Models.ThreadRead
{
    /// <summary>
    /// How far one member has read one thread. Posts are shown oldest first,
    /// so everything created at or before LastReadPostAt counts as read and
    /// anything newer is unread. The position only ever moves forward (see
    /// IThreadReadRepository.MarkRead).
    /// </summary>
    public class ThreadReadModel
    {
        public int ThreadId { get; set; }
        public string UserId { get; set; } = string.Empty;

        /// <summary>The newest post they have scrolled past.</summary>
        public int LastReadPostId { get; set; }

        /// <summary>That post's CreatedAt, which is what "unread" is measured against.</summary>
        public DateTime LastReadPostAt { get; set; }

        /// <summary>When the position last moved.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
