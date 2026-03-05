namespace MvcBB.Shared.Models.Post
{
    public class PostResponse
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int ThreadId { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByUserId { get; set; }
        public bool IsDeleted { get; set; }
        public string? DeletedByUserId { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeleteReason { get; set; }
        public bool IsEdited { get; set; }
        public string EditReason { get; set; } = string.Empty;
        public int? QuotedPostId { get; set; }
        public PostResponse? QuotedPost { get; set; }

        // Additional properties for UI display
        public string CreatedByUsername { get; set; } = string.Empty;
        public string? UpdatedByUsername { get; set; }
        public string? DeletedByUsername { get; set; }
    }
} 