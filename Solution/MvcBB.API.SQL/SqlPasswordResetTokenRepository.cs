using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed password reset token repository, calling the
    /// usp_PasswordResetToken_* stored procedures in Database/SQL/Stored
    /// Procedures.
    /// </summary>
    public class SqlPasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlPasswordResetTokenRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PasswordResetToken Add(PasswordResetToken token)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<PasswordResetToken>("dbo.usp_PasswordResetToken_Insert", new
            {
                token.UserId,
                token.TokenHash,
                token.ExpiresAt,
                token.CreatedAt
            }, commandType: CommandType.StoredProcedure);
        }

        public PasswordResetToken? GetByTokenHash(string tokenHash)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<PasswordResetToken>("dbo.usp_PasswordResetToken_GetByTokenHash", new { TokenHash = tokenHash }, commandType: CommandType.StoredProcedure);
        }

        public void MarkUsed(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_PasswordResetToken_MarkUsed", new { Id = id }, commandType: CommandType.StoredProcedure);
        }
    }
}
