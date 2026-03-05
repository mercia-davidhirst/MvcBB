using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Post
{
    public class Post
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Post content is required")]
        [StringLength(10000, ErrorMessage = "Content cannot exceed 10000 characters")]
        public string Content { get; set; } = string.Empty;

        public int ThreadId { get; set; }
        public string ThreadTitle { get; set; } = string.Empty;

        // Creation info
        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Edit info
        public bool IsEdited { get; set; }
        public string? EditReason { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByUserId { get; set; }
        public string? UpdatedByUsername { get; set; }

        // Deletion info
        public bool IsDeleted { get; set; }
        public string? DeletedByUserId { get; set; }
        public string? DeletedByUsername { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeleteReason { get; set; }

        // Quote info
        public int? QuotedPostId { get; set; }
        public Post? QuotedPost { get; set; }

        // User info
        public string? UserAvatar { get; set; }
        public string? UserSignature { get; set; }
        public int UserPostCount { get; set; }
        public DateTime UserJoinedAt { get; set; }
        public string? UserRole { get; set; }
    }
} 