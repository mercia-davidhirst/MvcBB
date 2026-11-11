using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.App.Services
{
    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        // Every report endpoint on the API requires the caller's own identity -
        // either a specific role (Administrator/Moderator) or the reporter's
        // NameIdentifier claim - so these all need the signed-in user's own token.
        private string GetCurrentUserToken()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
        }

        public async Task<IEnumerable<ReportResponse>> GetReportsAsync(ReportStatus? status = null)
        {
            var url = "api/reports";
            if (status.HasValue)
            {
                url += $"?status={status.Value}";
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ReportResponse>>() ?? Array.Empty<ReportResponse>();
        }

        public async Task<ReportResponse> GetReportAsync(int id)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.GetAsync($"api/reports/{id}");
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<ReportResponse>() ?? throw new Exception("Failed to load report");
        }

        public async Task<ReportResponse> CreateReportAsync(CreateReportRequest request)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.PostAsJsonAsync("api/reports", request);
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<ReportResponse>() ?? throw new Exception("Failed to create report");
        }

        public async Task<ReportResponse> UpdateReportAsync(int id, UpdateReportRequest request)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetCurrentUserToken());
            var response = await _httpClient.PutAsJsonAsync($"api/reports/{id}", request);
            response.EnsureSuccessOrSessionExpired();
            return await response.Content.ReadFromJsonAsync<ReportResponse>() ?? throw new Exception("Failed to update report");
        }
    }
}