namespace MvcBB.Shared.Models.Post
{
    /// <summary>
    /// Records that a user liked a post. One row per (PostId, UserId) pair -
    /// liking again just removes the row (toggle), it never duplicates.
    /// </summary>
    public class PostLike
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
