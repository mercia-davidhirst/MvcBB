using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed report repository, calling the fn_report_*/sp_report_*
    /// functions/procedures in Database/PostGresSql. Type/Status are SMALLINT
    /// columns there; C# enums default to an Int32 underlying type, so
    /// parameters are explicitly cast to short (Int16) to match - otherwise
    /// Npgsql can infer an int4 parameter type and PostgreSQL rejects the
    /// smallint/integer mismatch when resolving the function/procedure call.
    /// </summary>
    public class PostgreSqlReportRepository : IReportRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlReportRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<Report> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ReportRow>("SELECT * FROM public.fn_report_get_all()");
            return rows.Select(MapToReport).ToList();
        }

        public Report? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<ReportRow>("SELECT * FROM public.fn_report_get_by_id(@Id)", new { Id = id });
            return row == null ? null : MapToReport(row);
        }

        public Report Add(Report report)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_report_insert(@Reason, @ReporterUserId, @Type, @ContentId, @Status, @ModeratorNotes, NULL)",
                new
                {
                    report.Reason,
                    ReporterUserId = int.Parse(report.ReporterUserId),
                    Type = (short)report.Type,
                    report.ContentId,
                    Status = (short)report.Status,
                    report.ModeratorNotes
                });

            var row = connection.QuerySingle<ReportRow>("SELECT * FROM public.fn_report_get_by_id(@Id)", new { Id = newId });
            return MapToReport(row);
        }

        public void Update(Report report)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_report_update(@Id, @Status, @ModeratorNotes, @ResolvedAt, @ResolvedByUserId)",
                new
                {
                    report.Id,
                    Status = (short)report.Status,
                    report.ModeratorNotes,
                    report.ResolvedAt,
                    ResolvedByUserId = ParseUserId(report.ResolvedByUserId)
                });
        }

        public int Count()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_report_count()");
        }

        public int CountByStatus(ReportStatus status)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_report_count_by_status(@Status)", new { Status = (short)status });
        }

        public int CountByType(ReportType type)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_report_count_by_type(@Type)", new { Type = (short)type });
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private static Report MapToReport(ReportRow row) => new()
        {
            Id = row.Id,
            Reason = row.Reason,
            ReporterUserId = row.ReporterUserId.ToString(),
            CreatedAt = row.CreatedAt,
            Type = (ReportType)row.Type,
            ContentId = row.ContentId,
            Status = (ReportStatus)row.Status,
            ModeratorNotes = row.ModeratorNotes,
            ResolvedAt = row.ResolvedAt,
            ResolvedByUserId = row.ResolvedByUserId?.ToString() ?? string.Empty
        };

        /// <summary>
        /// Matches public.reports' actual column types (reporter_user_id/
        /// resolved_by_user_id are INTEGER foreign keys to users.id there, not
        /// strings as on the Report model; type/status are read as short since
        /// they're SMALLINT columns).
        /// </summary>
        private class ReportRow
        {
            public int Id { get; set; }
            public string Reason { get; set; } = string.Empty;
            public int ReporterUserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public short Type { get; set; }
            public int ContentId { get; set; }
            public short Status { get; set; }
            public string ModeratorNotes { get; set; } = string.Empty;
            public DateTime? ResolvedAt { get; set; }
            public int? ResolvedByUserId { get; set; }
        }
    }
}
