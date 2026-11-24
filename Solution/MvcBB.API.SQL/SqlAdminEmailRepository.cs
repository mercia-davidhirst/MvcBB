using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed admin email repository, calling the usp_AdminEmail_*
    /// and usp_AdminEmailOptOut_* stored procedures in Database/SQL/Stored
    /// Procedures.
    /// </summary>
    public class SqlAdminEmailRepository : IAdminEmailRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlAdminEmailRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public AdminEmail Add(AdminEmail email)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<AdminEmail>("dbo.usp_AdminEmail_Insert", new
            {
                email.SentByUserId,
                email.SentByUsername,
                CreatedAt = email.CreatedAt == default ? (DateTime?)null : email.CreatedAt,
                email.Subject,
                email.Body,
                Audience = (byte)email.Audience,
                Role = (byte?)email.Role,
                email.UserId,
                SenderMode = (byte)email.SenderMode,
                email.FromAddress,
                email.FromName,
                email.ReplyToAddress,
                email.IsImportant,
                Status = (byte)email.Status,
                email.RecipientCount
            }, commandType: CommandType.StoredProcedure);
        }

        public void UpdateProgress(AdminEmail email)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_AdminEmail_UpdateProgress", new
            {
                email.Id,
                Status = (byte)email.Status,
                email.RecipientCount,
                email.SentCount,
                email.FailedCount,
                email.CompletedAt
            }, commandType: CommandType.StoredProcedure);
        }

        public AdminEmail? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<AdminEmail>("dbo.usp_AdminEmail_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public IReadOnlyList<AdminEmail> GetRecent(int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AdminEmail>("dbo.usp_AdminEmail_GetRecent", new { Take = take }, commandType: CommandType.StoredProcedure).ToList();
        }

        public int MarkUnfinishedInterrupted()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_AdminEmail_MarkUnfinishedInterrupted", commandType: CommandType.StoredProcedure);
        }

        public bool IsOptedOut(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>("dbo.usp_AdminEmailOptOut_Get", new { UserId = userId }, commandType: CommandType.StoredProcedure);
        }

        public void SetOptedOut(int userId, bool optedOut)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_AdminEmailOptOut_Set", new { UserId = userId, OptedOut = optedOut }, commandType: CommandType.StoredProcedure);
        }

        public IReadOnlyList<AdminEmailRecipient> GetRecipients(UserRole? role, int? userId, bool includeOptedOut)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AdminEmailRecipient>(
                "dbo.usp_AdminEmail_GetRecipients",
                new { Role = (byte?)role, UserId = userId, IncludeOptedOut = includeOptedOut },
                commandType: CommandType.StoredProcedure).ToList();
        }

        public (int Total, int OptedOut) CountRecipients(UserRole? role, int? userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingle<CountRow>(
                "dbo.usp_AdminEmail_CountRecipients",
                new { Role = (byte?)role, UserId = userId },
                commandType: CommandType.StoredProcedure);
            return (row.Total, row.OptedOut);
        }

        private class CountRow
        {
            public int Total { get; set; }
            public int OptedOut { get; set; }
        }
    }
}
