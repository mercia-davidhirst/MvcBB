namespace MvcBB.Shared.Models.Board
{
    public class BoardResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }
        public string LastPostByUserId { get; set; } = string.Empty;
        public DateTime? LastPostAt { get; set; }

        // Additional properties for UI display
        public string LastPostByUsername { get; set; } = string.Empty;
        public string LastThreadTitle { get; set; } = string.Empty;
        public int LastThreadId { get; set; }
        public int LastPostId { get; set; }
    }
} 