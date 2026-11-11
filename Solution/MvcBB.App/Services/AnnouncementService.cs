using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Announcement;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Client for the announcement management endpoints on MvcBB.API.
    /// </summary>
    public class AnnouncementService : IAnnouncementService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AnnouncementService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<AnnouncementModel>> GetAnnouncementsAsync()
        {
            var response = await _httpClient.GetAsync("api/announcements");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<AnnouncementModel>>(content, _jsonOptions)
                ?? Array.Empty<AnnouncementModel>();
        }

        public async Task<IEnumerable<AnnouncementModel>> GetActiveAnnouncementsAsync()
        {
            var response = await _httpClient.GetAsync("api/announcements/active");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<AnnouncementModel>>(content, _jsonOptions)
                ?? Array.Empty<AnnouncementModel>();
        }

        public async Task<AnnouncementModel?> GetAnnouncementAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/announcements/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AnnouncementModel>(content, _jsonOptions);
        }

        public async Task AddAnnouncementAsync(AnnouncementModel model)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/announcements", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task UpdateAnnouncementAsync(int id, AnnouncementModel model)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/announcements/{id}", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task DeleteAnnouncementAsync(int id)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync($"api/announcements/{id}");
            response.EnsureSuccessOrSessionExpired();
        }

        // Announcement management endpoints require a real Administrator - the
        // generic mvc_client service token only carries the ApiClient role and gets
        // a 403, so these need the signed-in admin's own token.
        private string GetCurrentUserToken()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
        }

        // A 400 from these endpoints carries a specific validation message in its
        // ValidationProblemDetails body - EnsureSuccessStatusCode would discard that
        // and leave the caller with a generic failure, so extract it here.
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
