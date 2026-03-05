using MvcBB.Shared.Models.Search;

namespace MvcBB.App.Interfaces
{
    public interface ISearchService
    {
        Task<SearchResponse> SearchAsync(string query, string type = null, int? boardId = null, int page = 1, int pageSize = 20);
    }
} 