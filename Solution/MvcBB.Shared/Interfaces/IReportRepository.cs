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

        /// <summary>
        /// Reports still waiting on a moderator: status Pending or
        /// Investigating (not yet Resolved or Dismissed). Drives the navbar
        /// alert badge for moderators and administrators.
        /// </summary>
        int CountOpen();
    }
}
