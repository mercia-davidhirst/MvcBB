namespace MvcBB.Shared.Models.Search
{
    public class SearchResult
    {
        public string Type { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? ThreadId { get; set; }
        public string? ThreadTitle { get; set; }
        public int? BoardId { get; set; }
        public string? BoardName { get; set; }
    }
} 