using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Message;

namespace MvcBB.App.Services
{
    public class MessageService : IMessageService
    {
        private readonly HttpClient _httpClient;

        public MessageService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
        }

        public async Task<IEnumerable<MessageResponse>> GetMessagesAsync(string folder = "inbox")
        {
            var response = await _httpClient.GetAsync($"api/messages?folder={folder}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<MessageResponse>>() ?? Array.Empty<MessageResponse>();
        }

        public async Task<MessageResponse> GetMessageAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/messages/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MessageResponse>() ?? throw new Exception("Failed to load message");
        }

        public async Task<MessageResponse> CreateMessageAsync(CreateMessageRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/messages", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MessageResponse>() ?? throw new Exception("Failed to create message");
        }

        public async Task DeleteMessageAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/messages/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
} 