using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed admin email repository, calling the fn_admin_email_*/
    /// sp_admin_email_* functions/procedures in Database/PostGresSql.
    /// sp_admin_email_insert only returns the new row's id, so Add() re-fetches
    /// the full row afterwards.
    /// </summary>
    public class PostgreSqlAdminEmailRepository : IAdminEmailRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlAdminEmailRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public AdminEmail Add(AdminEmail email)
        {
            using var connection = _connectionFactory.CreateConnection();
            var id = connection.QuerySingle<int>(
                "CALL public.sp_admin_email_insert(@SentByUserId, @SentByUsername, @Subject, @Body, @Audience, @Role, @UserId, @SenderMode, @FromAddress, @FromName, @ReplyToAddress, @IsImportant, @Status, @RecipientCount, NULL)",
                new
                {
                    email.SentByUserId,
                    email.SentByUsername,
                    email.Subject,
                    email.Body,
                    Audience = (short)email.Audience,
                    Role = (short?)email.Role,
                    email.UserId,
                    SenderMode = (short)email.SenderMode,
                    email.FromAddress,
                    email.FromName,
                    email.ReplyToAddress,
                    email.IsImportant,
                    Status = (short)email.Status,
                    email.RecipientCount
                });

            return GetById(id) ?? throw new InvalidOperationException($"Admin email {id} was not found after insert");
        }

        public void UpdateProgress(AdminEmail email)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_admin_email_update_progress(@Id, @Status, @RecipientCount, @SentCount, @FailedCount, @CompletedAt, @PausedUntil)",
                new
                {
                    email.Id,
                    Status = (short)email.Status,
                    email.RecipientCount,
                    email.SentCount,
                    email.FailedCount,
                    email.CompletedAt,
                    email.PausedUntil
                });
        }

        public AdminEmail? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<AdminEmail>(
                "SELECT * FROM public.fn_admin_email_get_by_id(@Id)",
                new { Id = id });
        }

        public IReadOnlyList<AdminEmail> GetRecent(int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AdminEmail>(
                "SELECT * FROM public.fn_admin_email_get_recent(@Take)",
                new { Take = take }).ToList();
        }

        public int MarkUnfinishedInterrupted()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_admin_email_mark_unfinished_interrupted()");
        }

        public void AddFailure(AdminEmailFailure failure)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_admin_email_failure_insert(@AdminEmailId, @UserId, @Username, @Email, @Reason, @FailedAt)",
                new
                {
                    failure.AdminEmailId,
                    failure.UserId,
                    failure.Username,
                    failure.Email,
                    failure.Reason,
                    FailedAt = failure.FailedAt == default ? (DateTime?)null : failure.FailedAt
                });
        }

        public IReadOnlyList<AdminEmailFailure> GetFailures(int adminEmailId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AdminEmailFailure>(
                "SELECT * FROM public.fn_admin_email_failure_get_by_admin_email(@AdminEmailId)",
                new { AdminEmailId = adminEmailId }).ToList();
        }

        public void AddDelivery(int adminEmailId, int userId, DateTime sentAt)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_admin_email_delivery_insert(@AdminEmailId, @UserId, @SentAt)",
                new
                {
                    AdminEmailId = adminEmailId,
                    UserId = userId,
                    SentAt = sentAt == default ? (DateTime?)null : sentAt
                });
        }

        public IReadOnlyList<int> GetDeliveredUserIds(int adminEmailId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<int>(
                "SELECT * FROM public.fn_admin_email_delivery_get_user_ids(@AdminEmailId)",
                new { AdminEmailId = adminEmailId }).ToList();
        }

        public bool IsOptedOut(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>("SELECT public.fn_admin_email_opt_out_get(@UserId)", new { UserId = userId });
        }

        public void SetOptedOut(int userId, bool optedOut)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_admin_email_opt_out_set(@UserId, @OptedOut)", new { UserId = userId, OptedOut = optedOut });
        }

        public IReadOnlyList<AdminEmailRecipient> GetRecipients(UserRole? role, int? userId, bool includeOptedOut)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<AdminEmailRecipient>(
                "SELECT * FROM public.fn_admin_email_get_recipients(@Role, @UserId, @IncludeOptedOut)",
                new { Role = (short?)role, UserId = userId, IncludeOptedOut = includeOptedOut }).ToList();
        }

        public (int Total, int OptedOut) CountRecipients(UserRole? role, int? userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingle<CountRow>(
                "SELECT * FROM public.fn_admin_email_count_recipients(@Role, @UserId)",
                new { Role = (short?)role, UserId = userId });
            return (row.Total, row.OptedOut);
        }

        private class CountRow
        {
            public int Total { get; set; }
            public int OptedOut { get; set; }
        }
    }
}
