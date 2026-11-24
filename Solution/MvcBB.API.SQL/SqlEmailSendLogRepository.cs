using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed email send log, calling the usp_EmailSendLog_* stored
    /// procedures in Database/SQL/Stored Procedures.
    /// </summary>
    public class SqlEmailSendLogRepository : IEmailSendLogRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlEmailSendLogRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Add(DateTime sentAtUtc, EmailKind kind)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_EmailSendLog_Insert", new
            {
                SentAt = sentAtUtc == default ? (DateTime?)null : sentAtUtc,
                Kind = (byte)kind
            }, commandType: CommandType.StoredProcedure);
        }

        public IReadOnlyList<EmailSendCount> GetCounts(DateTime fromUtc, DateTime toUtc, int bucketMinutes)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<EmailSendCount>(
                "dbo.usp_EmailSendLog_GetCounts",
                new { From = fromUtc, To = toUtc, BucketMinutes = bucketMinutes },
                commandType: CommandType.StoredProcedure).ToList();
        }

        public IReadOnlyList<DateTime> GetSentTimesSince(DateTime sinceUtc)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<DateTime>("dbo.usp_EmailSendLog_GetSentSince", new { Since = sinceUtc }, commandType: CommandType.StoredProcedure).ToList();
        }

        public int Prune(DateTime beforeUtc)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_EmailSendLog_Prune", new { Before = beforeUtc }, commandType: CommandType.StoredProcedure);
        }
    }
}
