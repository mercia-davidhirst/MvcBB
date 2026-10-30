using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Client for the BBCode tag/smilie management endpoints on MvcBB.API.
    /// </summary>
    public class BBCodeService : IMvcBBCodeService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BBCodeService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
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
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/bbcode/tags", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task UpdateBBCodeTagAsync(int id, BBCodeTagModel model)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/bbcode/tags/{id}", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task DeleteBBCodeTagAsync(int id)
        {
            var token = GetCurrentUserToken();
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
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/bbcode/smilies", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task UpdateSmilieAsync(int id, SmilieModel model)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/bbcode/smilies/{id}", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task DeleteSmilieAsync(int id)
        {
            var token = GetCurrentUserToken();
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

        // All BBCode/smilie management endpoints require a real Administrator - the
        // generic mvc_client service token only carries the ApiClient role and gets
        // a 403, so these need the signed-in admin's own token.
        private string GetCurrentUserToken()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
        }

        // A 400 from these endpoints carries a specific validation message (e.g. a
        // duplicate code/name) in its ValidationProblemDetails body - EnsureSuccessStatusCode
        // would discard that and leave the caller with a generic failure, so extract it here.
        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var document = JsonDocument.Parse(body);
                // A framework-level validation failure wraps field errors in "errors"
                // (ValidationProblemDetails); an action that calls BadRequest(ModelState)
                // itself after ModelState was already valid returns that dictionary
                // directly at the root instead. Handle both shapes.
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
