using MvcBB.Shared.Models.Report;

namespace MvcBB.Shared.Interfaces
{
    public interface IReportRepository
    {
        IReadOnlyList<Report> GetAll();
        Report? GetById(int id);
        Report Add(Report report);
        void Update(Report report);
        int Count();
        int CountByStatus(ReportStatus status);
        int CountByType(ReportType type);
    }
}
