using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.API.SQL
{
    public class SqlReportRepository : IReportRepository
    {
        public IReadOnlyList<Report> GetAll() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public Report? GetById(int id) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public Report Add(Report report) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Update(Report report) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public int Count() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public int CountByStatus(ReportStatus status) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public int CountByType(ReportType type) => throw new NotImplementedException("Implement with EF Core or Dapper");
    }
}
