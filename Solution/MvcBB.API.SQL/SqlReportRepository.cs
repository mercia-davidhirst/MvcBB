using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed report repository, calling the usp_Report_* stored
    /// procedures in Database/SQL/Stored Procedures.
    /// </summary>
    public class SqlReportRepository : IReportRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlReportRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<Report> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ReportRow>("dbo.usp_Report_GetAll", commandType: CommandType.StoredProcedure);
            return rows.Select(MapToReport).ToList();
        }

        public Report? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<ReportRow>("dbo.usp_Report_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
            return row == null ? null : MapToReport(row);
        }

        public Report Add(Report report)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingle<ReportRow>("dbo.usp_Report_Insert", new
            {
                report.Reason,
                ReporterUserId = int.Parse(report.ReporterUserId),
                report.CreatedAt,
                report.Type,
                report.ContentId,
                report.Status,
                report.ModeratorNotes
            }, commandType: CommandType.StoredProcedure);
            return MapToReport(row);
        }

        public void Update(Report report)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Report_Update", new
            {
                report.Id,
                report.Status,
                report.ModeratorNotes,
                report.ResolvedAt,
                ResolvedByUserId = ParseUserId(report.ResolvedByUserId)
            }, commandType: CommandType.StoredProcedure);
        }

        public int Count()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_Report_Count", commandType: CommandType.StoredProcedure);
        }

        public int CountByStatus(ReportStatus status)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_Report_CountByStatus", new { Status = status }, commandType: CommandType.StoredProcedure);
        }

        public int CountByType(ReportType type)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_Report_CountByType", new { Type = type }, commandType: CommandType.StoredProcedure);
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private static Report MapToReport(ReportRow row) => new()
        {
            Id = row.Id,
            Reason = row.Reason,
            ReporterUserId = row.ReporterUserId.ToString(),
            CreatedAt = row.CreatedAt,
            Type = row.Type,
            ContentId = row.ContentId,
            Status = row.Status,
            ModeratorNotes = row.ModeratorNotes,
            ResolvedAt = row.ResolvedAt,
            ResolvedByUserId = row.ResolvedByUserId?.ToString() ?? string.Empty
        };

        /// <summary>
        /// Matches dbo.Reports' actual column types (ReporterUserId/
        /// ResolvedByUserId are INT foreign keys to Users.Id there, not
        /// strings as on the Report model).
        /// </summary>
        private class ReportRow
        {
            public int Id { get; set; }
            public string Reason { get; set; } = string.Empty;
            public int ReporterUserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public ReportType Type { get; set; }
            public int ContentId { get; set; }
            public ReportStatus Status { get; set; }
            public string ModeratorNotes { get; set; } = string.Empty;
            public DateTime? ResolvedAt { get; set; }
            public int? ResolvedByUserId { get; set; }
        }
    }
}
