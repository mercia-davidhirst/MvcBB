using MvcBB.Shared.Models.Report;

namespace MvcBB.App.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<ReportResponse>> GetReportsAsync(ReportStatus? status = null);
        Task<ReportResponse> GetReportAsync(int id);
        Task<ReportResponse> CreateReportAsync(CreateReportRequest request);
        Task<ReportResponse> UpdateReportAsync(int id, UpdateReportRequest request);
    }
} 