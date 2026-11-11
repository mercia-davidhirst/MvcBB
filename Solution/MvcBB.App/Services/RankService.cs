using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Rank;

namespace MvcBB.App.Services
{
    /// <summary>Client for the rank management endpoints on MvcBB.API.</summary>
    public class RankService : IRankService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RankService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<RankModel>> GetRanksAsync()
        {
            var response = await _httpClient.GetAsync("api/ranks");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<RankModel>>(content, _jsonOptions)
                ?? Array.Empty<RankModel>();
        }

        public async Task<RankModel?> GetRankAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/ranks/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RankModel>(content, _jsonOptions);
        }

        public async Task AddRankAsync(RankModel model)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/ranks", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task UpdateRankAsync(int id, RankModel model)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/ranks/{id}", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task DeleteRankAsync(int id)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync($"api/ranks/{id}");
            response.EnsureSuccessOrSessionExpired();
        }

        private string GetCurrentUserToken()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
        }

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new SessionExpiredException();
            }

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var document = JsonDocument.Parse(body);
                var errorsElement = document.RootElement.TryGetProperty("errors", out var errors)
                    ? errors
                    : document.RootElement;

                if (errorsElement.ValueKind == JsonValueKind.Object)
                {
                    var messages = errorsElement.EnumerateObject()
                        .Where(prop => prop.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(prop => prop.Value.EnumerateArray().Select(v => v.GetString()))
                        .Where(m => !string.IsNullOrEmpty(m));
                    var message = string.Join(" ", messages);
                    if (!string.IsNullOrEmpty(message))
                    {
                        throw new ServiceException(message);
                    }
                }
            }
            catch (JsonException)
            {
                // Body wasn't a ValidationProblemDetails payload - fall through to the generic exception below.
            }

            response.EnsureSuccessStatusCode();
        }
    }
}
