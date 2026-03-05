namespace MvcBB.API.Models.Search
{
    public class SearchResult
    {
        public string Type { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BoardId { get; set; }
        public string BoardName { get; set; }
        public int? ThreadId { get; set; }
        public string ThreadTitle { get; set; }
    }
} 