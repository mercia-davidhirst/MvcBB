using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.App.Services
{
    public class AdminEmailService : IAdminEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminEmailService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        // Every api/admin-emails endpoint needs a real Administrator (it sends
        // as them), so always the signed-in user's own token.
        private void Authorize()
        {
            var token = _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<AdminEmailPreview> PreviewAsync(SendAdminEmailRequest request)
        {
            Authorize();
            var response = await _httpClient.PostAsJsonAsync("api/admin-emails/preview", request);
            await EnsureSuccessOrThrowAsync(response);
            return await response.Content.ReadFromJsonAsync<AdminEmailPreview>() ?? throw new Exception("Failed to read the email preview");
        }

        public async Task<AdminEmail> SendAsync(SendAdminEmailRequest request)
        {
            Authorize();
            var response = await _httpClient.PostAsJsonAsync("api/admin-emails", request);
            await EnsureSuccessOrThrowAsync(response);
            return await response.Content.ReadFromJsonAsync<AdminEmail>() ?? throw new Exception("Failed to read the queued email");
        }

        public async Task<string> SendTestAsync(SendAdminEmailRequest request)
        {
            Authorize();
            var response = await _httpClient.PostAsJsonAsync("api/admin-emails/test", request);
            await EnsureSuccessOrThrowAsync(response);
            return ReadMessage(await response.Content.ReadAsStringAsync()) ?? "Test email sent.";
        }

        public async Task<IEnumerable<AdminEmail>> GetRecentAsync(int take = 50)
        {
            Authorize();
            var response = await _httpClient.GetAsync($"api/admin-emails?take={take}");
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<IEnumerable<AdminEmail>>() ?? Array.Empty<AdminEmail>();
        }

        public async Task<AdminEmail?> GetAsync(int id)
        {
            Authorize();
            var response = await _httpClient.GetAsync($"api/admin-emails/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<AdminEmail>();
        }

        // A 400 carries a specific reason ("Choose a role to send to.", or the
        // model validation errors) - surface it as a ServiceException the
        // controller can show, instead of a generic failure.
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

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var message = ReadMessage(await response.Content.ReadAsStringAsync());
                if (!string.IsNullOrEmpty(message))
                {
                    throw new ServiceException(message);
                }
            }

            response.EnsureSuccessStatusCode();
        }

        private static string? ReadMessage(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                if (root.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                {
                    return messageProp.GetString();
                }

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .Where(prop => prop.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(prop => prop.Value.EnumerateArray().Select(v => v.GetString()))
                        .Where(m => !string.IsNullOrEmpty(m));
                    var joined = string.Join(" ", messages);
                    return string.IsNullOrEmpty(joined) ? null : joined;
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }
    }
}
