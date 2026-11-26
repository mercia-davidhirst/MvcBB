using System.Net;
using System.Net.Http.Headers;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Notification;

namespace MvcBB.App.Services
{
    public class NotificationsService : INotificationsService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        // Notifications are per member - the API works out whose from the token.
        private void AttachUserToken()
        {
            var token = _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<NotificationPage> GetPageAsync(int page = 1, int pageSize = 25)
        {
            AttachUserToken();
            var response = await _httpClient.GetAsync($"api/notifications?page={page}&pageSize={pageSize}");
            await response.EnsureSuccessWithMessageAsync();
            return await response.Content.ReadFromJsonAsync<NotificationPage>() ?? new NotificationPage();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            AttachUserToken();
            var response = await _httpClient.GetAsync("api/notifications/unread-count");
            response.EnsureSuccessOrSessionExpired();
            var result = await response.Content.ReadFromJsonAsync<UnreadCountResponse>();
            return result?.UnreadCount ?? 0;
        }

        public async Task<Notification?> MarkReadAsync(int id)
        {
            AttachUserToken();
            var response = await _httpClient.PostAsync($"api/notifications/{id}/read", content: null);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            await response.EnsureSuccessWithMessageAsync();
            return await response.Content.ReadFromJsonAsync<Notification>();
        }

        public async Task MarkAllReadAsync()
        {
            AttachUserToken();
            var response = await _httpClient.PostAsync("api/notifications/read-all", content: null);
            await response.EnsureSuccessWithMessageAsync();
        }

        public async Task<IReadOnlyList<string>> GetMentionSuggestionsAsync(string query)
        {
            AttachUserToken();
            var response = await _httpClient.GetAsync($"api/users/mention-suggestions?q={Uri.EscapeDataString(query)}");
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
        }

        private class UnreadCountResponse
        {
            public int UnreadCount { get; set; }
        }
    }
}
