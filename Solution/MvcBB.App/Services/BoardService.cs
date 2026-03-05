using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.Authorization;

namespace MvcBB.App.Services
{
    public class BoardService : IBoardService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BoardService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _authService = authService;
        }

        public async Task<IEnumerable<BoardResponse>> GetBoardsAsync()
        {
            var response = await _httpClient.GetAsync("api/boards");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<BoardResponse>>(content, _jsonOptions) 
                ?? Array.Empty<BoardResponse>();
        }

        public async Task<BoardResponse> GetBoardAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/boards/{id}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BoardResponse>(content, _jsonOptions) 
                ?? throw new Exception($"Failed to get board with ID {id}");
        }

        public async Task<BoardResponse> CreateBoardAsync(CreateBoardRequest request)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/boards", content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BoardResponse>(responseContent, _jsonOptions) 
                ?? throw new Exception("Failed to create board");
        }

        public async Task UpdateBoardAsync(int id, UpdateBoardRequest request)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/boards/{id}", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteBoardAsync(int id)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync($"api/boards/{id}");
            response.EnsureSuccessStatusCode();
        }

        private async Task<string> GetTokenAsync()
        {
            var credentials = new ApiCredentials
            {
                ClientId = "mvc_client",
                ClientSecret = "your_secure_secret_here"
            };
            var response = await _authService.GetTokenAsync(credentials);
            return response.Token;
        }
    }
} 