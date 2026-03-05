using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Thread
{
    public class ForumThread
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public int BoardId { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }
    }
} 