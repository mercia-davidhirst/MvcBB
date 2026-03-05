using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.ForumThread
{
    public class ForumThread
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        public int BoardId { get; set; }
        public required string CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }

        public int ViewCount { get; set; }
        public int PostCount { get; set; }
        
        public DateTime? LastPostAt { get; set; }
        public string? LastPostByUserId { get; set; }
    }
} 