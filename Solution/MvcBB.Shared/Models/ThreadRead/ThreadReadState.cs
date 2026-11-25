namespace MvcBB.Shared.Models.ThreadRead
{
    /// <summary>
    /// The signed-in viewer's read position in a thread, sent with a single
    /// thread (ThreadResponse.ReadState). Null for guests.
    /// </summary>
    public class ThreadReadState
    {
        /// <summary>Null when they have never opened the thread before.</summary>
        public int? LastReadPostId { get; set; }

        /// <summary>Posts created after this are unread. Null when they have never opened the thread before.</summary>
        public DateTime? LastReadPostAt { get; set; }

        /// <summary>The viewer's profile choice to scroll straight to the first unread post.</summary>
        public bool AutoJumpToUnread { get; set; } = true;
    }
}
