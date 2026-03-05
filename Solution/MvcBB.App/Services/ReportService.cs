using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.App.Services
{
    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;

        public ReportService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
        }

        public async Task<IEnumerable<ReportResponse>> GetReportsAsync(ReportStatus? status = null)
        {
            var url = "api/reports";
            if (status.HasValue)
            {
                url += $"?status={status.Value}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ReportResponse>>() ?? Array.Empty<ReportResponse>();
        }

        public async Task<ReportResponse> GetReportAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/reports/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReportResponse>() ?? throw new Exception("Failed to load report");
        }

        public async Task<ReportResponse> CreateReportAsync(CreateReportRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/reports", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReportResponse>() ?? throw new Exception("Failed to create report");
        }

        public async Task<ReportResponse> UpdateReportAsync(int id, UpdateReportRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/reports/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReportResponse>() ?? throw new Exception("Failed to update report");
        }
    }
} 