using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.App.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SettingsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
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
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());

            var json = JsonSerializer.Serialize(settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("api/settings/display", content);
            response.EnsureSuccessOrSessionExpired();
        }

        public async Task<PasswordPolicy> GetPasswordPolicyAsync()
        {
            var response = await _httpClient.GetAsync("api/settings/password-policy");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PasswordPolicy>(content, _jsonOptions)
                ?? new PasswordPolicy();
        }

        public async Task UpdatePasswordPolicyAsync(PasswordPolicy policy)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());

            var json = JsonSerializer.Serialize(policy);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("api/settings/password-policy", content);
            response.EnsureSuccessOrSessionExpired();
        }

        // Changing settings needs the signed-in admin's own token - the shared
        // service token is read-only and the API refuses it here.
        private string GetCurrentUserToken()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
        }
    }
}
