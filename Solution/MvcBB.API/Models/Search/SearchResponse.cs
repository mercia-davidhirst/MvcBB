namespace MvcBB.API.Models.Search
{
    public class SearchResponse
    {
        public List<SearchResult> Results { get; set; } = new();
        public int TotalResults { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalResults / (double)PageSize);
    }
} 