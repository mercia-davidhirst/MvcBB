using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.API.InMemory
{
    public class InMemoryReportRepository : IReportRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryReportRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<Report> GetAll() => _store.Reports.OrderByDescending(r => r.CreatedAt).ToList();

        public Report? GetById(int id) => _store.Reports.FirstOrDefault(r => r.Id == id);

        public Report Add(Report report)
        {
            if (report.Id == 0)
                report.Id = _store.Reports.Count == 0 ? 1 : _store.Reports.Max(r => r.Id) + 1;
            _store.Reports.Add(report);
            return report;
        }

        public void Update(Report report)
        {
            var index = _store.Reports.FindIndex(r => r.Id == report.Id);
            if (index >= 0)
                _store.Reports[index] = report;
        }

        public int Count() => _store.Reports.Count;

        public int CountByStatus(ReportStatus status) => _store.Reports.Count(r => r.Status == status);

        public int CountByType(ReportType type) => _store.Reports.Count(r => r.Type == type);
    }
}
