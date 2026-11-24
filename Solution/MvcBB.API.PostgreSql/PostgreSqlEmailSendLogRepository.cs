using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed email send log, calling the fn_email_send_log_*/
    /// sp_email_send_log_* functions/procedures in Database/PostGresSql.
    /// </summary>
    public class PostgreSqlEmailSendLogRepository : IEmailSendLogRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlEmailSendLogRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Add(DateTime sentAtUtc, EmailKind kind)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_email_send_log_insert(@SentAt, @Kind)",
                new
                {
                    SentAt = sentAtUtc == default ? (DateTime?)null : DateTime.SpecifyKind(sentAtUtc, DateTimeKind.Utc),
                    Kind = (short)kind
                });
        }

        public IReadOnlyList<EmailSendCount> GetCounts(DateTime fromUtc, DateTime toUtc, int bucketMinutes)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<EmailSendCount>(
                "SELECT * FROM public.fn_email_send_log_get_counts(@From, @To, @BucketMinutes)",
                new
                {
                    From = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc),
                    To = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc),
                    BucketMinutes = bucketMinutes
                }).ToList();
        }

        public IReadOnlyList<DateTime> GetSentTimesSince(DateTime sinceUtc)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<DateTime>(
                "SELECT * FROM public.fn_email_send_log_get_sent_since(@Since)",
                new { Since = DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc) }).ToList();
        }

        public int Prune(DateTime beforeUtc)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>(
                "SELECT public.fn_email_send_log_prune(@Before)",
                new { Before = DateTime.SpecifyKind(beforeUtc, DateTimeKind.Utc) });
        }
    }
}
