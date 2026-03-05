using System.Text.Json;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Search;

namespace MvcBB.App.Services
{
    public class SearchService : ISearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SearchService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _authService = authService;
        }

        public async Task<SearchResponse> SearchAsync(string query, string type = null, int? boardId = null, int page = 1, int pageSize = 20)
        {
            var queryParams = new List<string>
            {
                $"query={Uri.EscapeDataString(query)}",
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrEmpty(type))
            {
                queryParams.Add($"type={Uri.EscapeDataString(type)}");
            }

            if (boardId.HasValue)
            {
                queryParams.Add($"boardId={boardId.Value}");
            }

            var url = $"api/search?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SearchResponse>(content, _jsonOptions) 
                ?? throw new Exception("Failed to deserialize search response");
        }
    }
} 