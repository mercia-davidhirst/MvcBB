using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlReportRepository : IReportRepository
    {
        public IReadOnlyList<Report> GetAll() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public Report? GetById(int id) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public Report Add(Report report) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Update(Report report) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public int Count() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public int CountByStatus(ReportStatus status) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public int CountByType(ReportType type) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
    }
}
