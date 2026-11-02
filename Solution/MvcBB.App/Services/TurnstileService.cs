using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MvcBB.App.Interfaces;

namespace MvcBB.App.Services
{
    public class TurnstileService : ITurnstileService
    {
        private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TurnstileService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public string? SiteKey => _configuration["Turnstile:SiteKey"];
        private string? SecretKey => _configuration["Turnstile:SecretKey"];

        public bool IsEnabled => !string.IsNullOrWhiteSpace(SiteKey) && !string.IsNullOrWhiteSpace(SecretKey);

        public async Task<bool> ValidateAsync(string? token, string? remoteIp)
        {
            if (!IsEnabled)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var form = new Dictionary<string, string>
            {
                ["secret"] = SecretKey!,
                ["response"] = token
            };
            if (!string.IsNullOrEmpty(remoteIp))
            {
                form["remoteip"] = remoteIp;
            }

            var response = await _httpClient.PostAsync(VerifyUrl, new FormUrlEncodedContent(form));
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>();
            return result?.Success ?? false;
        }

        private class TurnstileVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }
    }
}
