using System.Net.Http.Headers;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Moderation;

namespace MvcBB.App.Services
{
    public class ModerationService : IModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ModerationService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        // Every moderation endpoint needs a real Administrator/Moderator token.
        private void AttachUserToken()
        {
            var token = _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<IReadOnlyList<ModerationQueueItem>> GetQueueAsync()
        {
            AttachUserToken();
            var response = await _httpClient.GetAsync("api/moderation/queue");
            await response.EnsureSuccessWithMessageAsync();
            return await response.Content.ReadFromJsonAsync<List<ModerationQueueItem>>() ?? new List<ModerationQueueItem>();
        }

        public async Task<int> GetQueueCountAsync()
        {
            AttachUserToken();
            var response = await _httpClient.GetAsync("api/moderation/queue/count");
            response.EnsureSuccessOrSessionExpired();
            var result = await response.Content.ReadFromJsonAsync<CountResponse>();
            return result?.Count ?? 0;
        }

        public async Task<ApproveQueuedPostResponse> ApproveAsync(int id)
        {
            AttachUserToken();
            var response = await _httpClient.PostAsync($"api/moderation/queue/{id}/approve", content: null);
            await response.EnsureSuccessWithMessageAsync();
            return await response.Content.ReadFromJsonAsync<ApproveQueuedPostResponse>()
                ?? throw new ServiceException("The post was approved, but the response couldn't be read.");
        }

        public async Task<ModerationQueueItem> RejectAsync(int id, string? reason)
        {
            AttachUserToken();
            var response = await _httpClient.PostAsJsonAsync($"api/moderation/queue/{id}/reject", new RejectQueuedPostRequest { Reason = reason });
            await response.EnsureSuccessWithMessageAsync();
            return await response.Content.ReadFromJsonAsync<ModerationQueueItem>()
                ?? throw new ServiceException("The post was turned down, but the response couldn't be read.");
        }

        public async Task<ModerationLogPage> GetLogAsync(int page = 1, int pageSize = 50)
        {
            AttachUserToken();
            var response = await _httpClient.GetAsync($"api/moderation/log?page={page}&pageSize={pageSize}");
            await response.EnsureSuccessWithMessageAsync();
            return await response.Content.ReadFromJsonAsync<ModerationLogPage>() ?? new ModerationLogPage();
        }

        public async Task<ModerationSettings> GetSettingsAsync()
        {
            AttachUserToken();
            var response = await _httpClient.GetAsync("api/moderation/settings");
            await response.EnsureSuccessWithMessageAsync();
            return await response.Content.ReadFromJsonAsync<ModerationSettings>() ?? new ModerationSettings();
        }

        public async Task UpdateSettingsAsync(ModerationSettings settings)
        {
            AttachUserToken();
            var response = await _httpClient.PutAsJsonAsync("api/moderation/settings", settings);
            await response.EnsureSuccessWithMessageAsync();
        }

        private class CountResponse
        {
            public int Count { get; set; }
        }
    }
}
