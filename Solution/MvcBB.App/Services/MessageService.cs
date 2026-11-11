using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Message;

namespace MvcBB.App.Services
{
    public class MessageService : IMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MessageService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        // Private messages are inherently per-user - the API resolves the
        // recipient/sender from the caller's own NameIdentifier claim, so every
        // call here needs the signed-in user's own token.
        private string GetCurrentUserToken()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
        }

        public async Task<IEnumerable<MessageResponse>> GetMessagesAsync(string folder = "inbox")
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.GetAsync($"api/messages?folder={folder}");
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<IEnumerable<MessageResponse>>() ?? Array.Empty<MessageResponse>();
        }

        public async Task<MessageResponse> GetMessageAsync(int id)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.GetAsync($"api/messages/{id}");
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<MessageResponse>() ?? throw new Exception("Failed to load message");
        }

        public async Task<MessageResponse> CreateMessageAsync(CreateMessageRequest request)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.PostAsJsonAsync("api/messages", request);
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<MessageResponse>() ?? throw new Exception("Failed to create message");
        }

        public async Task DeleteMessageAsync(int id)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.DeleteAsync($"api/messages/{id}");
            response.EnsureSuccessOrSessionExpired();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.GetAsync("api/messages/unread-count");
            response.EnsureSuccessOrSessionExpired();
            var result = await response.Content.ReadFromJsonAsync<UnreadCountResponse>();
            return result?.UnreadCount ?? 0;
        }

        private class UnreadCountResponse
        {
            public int UnreadCount { get; set; }
        }
    }
}