using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.WordFilter;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Client for the word-filter management endpoints on MvcBB.API. Unlike
    /// BBCodeService's tag/smilie reads, every call here requires an admin
    /// token - WordFiltersController is [Authorize(Roles = "Administrator")]
    /// across the board, including GET, since the censor list itself
    /// shouldn't be readable by a visitor trying to word around it.
    /// </summary>
    public class WordFilterService : IWordFilterService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public WordFilterService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<WordFilterModel>> GetWordFiltersAsync()
        {
            AttachCurrentUserToken();
            var response = await _httpClient.GetAsync("api/wordfilters");
            response.EnsureSuccessOrSessionExpired();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<WordFilterModel>>(content, _jsonOptions)
                ?? Array.Empty<WordFilterModel>();
        }

        public async Task<WordFilterModel?> GetWordFilterAsync(int id)
        {
            AttachCurrentUserToken();
            var response = await _httpClient.GetAsync($"api/wordfilters/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessOrSessionExpired();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WordFilterModel>(content, _jsonOptions);
        }

        public async Task AddWordFilterAsync(WordFilterModel model)
        {
            AttachCurrentUserToken();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/wordfilters", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task UpdateWordFilterAsync(int id, WordFilterModel model)
        {
            AttachCurrentUserToken();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/wordfilters/{id}", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task DeleteWordFilterAsync(int id)
        {
            AttachCurrentUserToken();
            var response = await _httpClient.DeleteAsync($"api/wordfilters/{id}");
            response.EnsureSuccessOrSessionExpired();
        }

        private void AttachCurrentUserToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
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
