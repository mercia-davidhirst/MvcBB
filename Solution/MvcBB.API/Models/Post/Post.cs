using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Post
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public int ThreadId { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsEdited { get; set; }
        public string EditReason { get; set; }
        
        // Quote-related properties
        public int? QuotedPostId { get; set; }
        public Post QuotedPost { get; set; }
    }
} 