namespace MvcBB.App.Models
{
    public class PostDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int ThreadId { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public string EditReason { get; set; }
        public int? QuotedPostId { get; set; }
        public PostDto QuotedPost { get; set; }
    }
} 