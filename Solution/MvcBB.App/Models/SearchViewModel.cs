using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.Search;

namespace MvcBB.App.Models
{
    public class SearchViewModel
    {
        public string Query { get; set; }
        public string Type { get; set; }
        public int? BoardId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalResults { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<SearchResult> Results { get; set; } = Array.Empty<SearchResult>();
        public IEnumerable<BoardResponse> Boards { get; set; } = Array.Empty<BoardResponse>();
    }
} 