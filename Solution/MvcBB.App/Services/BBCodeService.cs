using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Authorization;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Client for the BBCode tag/smilie management endpoints on MvcBB.API.
    /// </summary>
    public class BBCodeService : IMvcBBCodeService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BBCodeService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _authService = authService;
        }

        public async Task<IEnumerable<BBCodeTagModel>> GetBBCodeTagsAsync()
        {
            var response = await _httpClient.GetAsync("api/bbcode/tags");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<BBCodeTagModel>>(content, _jsonOptions)
                ?? Array.Empty<BBCodeTagModel>();
        }

        public async Task<BBCodeTagModel?> GetBBCodeTagAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/bbcode/tags/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BBCodeTagModel>(content, _jsonOptions);
        }

        public async Task AddBBCodeTagAsync(BBCodeTagModel model)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/bbcode/tags", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateBBCodeTagAsync(int id, BBCodeTagModel model)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/bbcode/tags/{id}", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteBBCodeTagAsync(int id)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync($"api/bbcode/tags/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<SmilieModel>> GetSmiliesAsync()
        {
            var response = await _httpClient.GetAsync("api/bbcode/smilies");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<SmilieModel>>(content, _jsonOptions)
                ?? Array.Empty<SmilieModel>();
        }

        public async Task<SmilieModel?> GetSmilieAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/bbcode/smilies/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SmilieModel>(content, _jsonOptions);
        }

        public async Task AddSmilieAsync(SmilieModel model)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/bbcode/smilies", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateSmilieAsync(int id, SmilieModel model)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/bbcode/smilies/{id}", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteSmilieAsync(int id)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync($"api/bbcode/smilies/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<Dictionary<string, string>> GetAvailableSmiliesAsync()
        {
            var smilies = await GetSmiliesAsync();
            return smilies
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToDictionary(
                    s => s.Code,
                    s => $"<img src=\"{s.ImagePath}\" alt=\"{s.Description}\" class=\"smilie\" />"
                );
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
