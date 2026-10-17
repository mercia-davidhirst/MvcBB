using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed password reset token repository, calling the
    /// fn_password_reset_token_*/sp_password_reset_token_* functions/
    /// procedures in Database/PostGresSql. sp_password_reset_token_insert only
    /// returns the new row's id, so Add() re-fetches the full row afterwards
    /// via its (unique) token hash.
    /// </summary>
    public class PostgreSqlPasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlPasswordResetTokenRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PasswordResetToken Add(PasswordResetToken token)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_password_reset_token_insert(@UserId, @TokenHash, @ExpiresAt, NULL)",
                new { token.UserId, token.TokenHash, token.ExpiresAt });

            return connection.QuerySingle<PasswordResetToken>(
                "SELECT * FROM public.fn_password_reset_token_get_by_token_hash(@TokenHash)",
                new { token.TokenHash });
        }

        public PasswordResetToken? GetByTokenHash(string tokenHash)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<PasswordResetToken>(
                "SELECT * FROM public.fn_password_reset_token_get_by_token_hash(@TokenHash)",
                new { TokenHash = tokenHash });
        }

        public void MarkUsed(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_password_reset_token_mark_used(@Id)", new { Id = id });
        }
    }
}
