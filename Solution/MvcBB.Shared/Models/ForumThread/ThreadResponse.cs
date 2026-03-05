using MvcBB.Shared.Models.Post;

namespace MvcBB.Shared.Models.ForumThread
{
    public class ThreadResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int BoardId { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }
        public int ViewCount { get; set; }
        public int ReplyCount { get; set; }
        public DateTime? LastPostAt { get; set; }
        public string? LastPostByUserId { get; set; }

        // Additional properties for UI display
        public string CreatedByUsername { get; set; } = string.Empty;
        public string? LastPostByUsername { get; set; }
        public string BoardName { get; set; } = string.Empty;
        public PostResponse? FirstPost { get; set; }
        public PostResponse? LastPost { get; set; }
    }
} 