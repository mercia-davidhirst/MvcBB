namespace MvcBB.Shared.Models.Post
{
    /// <summary>
    /// Where a post sits in its discussion (see IPostRepository.GetThreadLocation).
    /// Posts are in order of CreatedAt, then Id.
    /// </summary>
    public class ThreadPostLocation
    {
        /// <summary>How many posts the discussion has (deleted ones included, as they keep their place).</summary>
        public int TotalCount { get; set; }

        /// <summary>The post that was looked for, or null when there's none (not in this thread, or nothing unread).</summary>
        public int? PostId { get; set; }

        /// <summary>That post's 0-based position in the discussion, or null.</summary>
        public int? Position { get; set; }
    }

    /// <summary>One page of a discussion's posts (GET api/posts/thread/{threadId}/page).</summary>
    public class ThreadPostPage
    {
        public List<PostResponse> Posts { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPosts { get; set; }
        public int TotalPages => TotalPosts == 0 ? 1 : (int)Math.Ceiling(TotalPosts / (double)PageSize);

        /// <summary>
        /// The first post the signed-in member hasn't read (newer than their
        /// saved position and not their own), wherever it is in the
        /// discussion, and the page it's on. Null when there's nothing unread
        /// or the member has no saved position yet.
        /// </summary>
        public int? FirstUnreadPostId { get; set; }
        public int? FirstUnreadPage { get; set; }
    }
}
