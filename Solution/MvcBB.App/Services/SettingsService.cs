using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Settings;
using MvcBB.Shared.Models.Authorization;

namespace MvcBB.App.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SettingsService(IHttpClientFactory httpClientFactory, IAuthService authService, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _authService = authService;
            _configuration = configuration;
        }

        public async Task<DisplaySettings> GetDisplaySettingsAsync()
        {
            var response = await _httpClient.GetAsync("api/settings/display");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DisplaySettings>(content, _jsonOptions)
                ?? new DisplaySettings();
        }

        public async Task UpdateDisplaySettingsAsync(DisplaySettings settings)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("api/settings/display", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<string> GetTokenAsync()
        {
            var credentials = new ApiCredentials
            {
                ClientId = _configuration["Api:ClientId"] ?? string.Empty,
                ClientSecret = _configuration["Api:ClientSecret"] ?? string.Empty
            };
            var response = await _authService.GetTokenAsync(credentials);
            return response.Token;
        }
    }
}
