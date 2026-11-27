using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed "Remember me" refresh token repository, calling the
    /// fn_refresh_token_*/sp_refresh_token_* functions and procedures in
    /// Database/PostGresSql. Column names map through
    /// Dapper.DefaultTypeMap.MatchNamesWithUnderscores.
    /// </summary>
    public class PostgreSqlRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlRefreshTokenRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        public RefreshToken Add(RefreshToken token)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<RefreshToken>(
                "SELECT * FROM public.fn_refresh_token_insert(@UserId, @TokenHash, @FamilyId, @SessionStamp, @CreatedAt, @ExpiresAt, @CreatedIp)",
                new
                {
                    token.UserId,
                    token.TokenHash,
                    token.FamilyId,
                    token.SessionStamp,
                    CreatedAt = Utc(token.CreatedAt),
                    ExpiresAt = Utc(token.ExpiresAt),
                    token.CreatedIp
                });
        }

        public RefreshToken? GetByTokenHash(string tokenHash)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<RefreshToken>(
                "SELECT * FROM public.fn_refresh_token_get_by_token_hash(@TokenHash)",
                new { TokenHash = tokenHash });
        }

        public bool MarkUsed(int id, DateTime usedAt)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>(
                "SELECT public.fn_refresh_token_mark_used(@Id, @UsedAt)",
                new { Id = id, UsedAt = Utc(usedAt) });
        }

        public void RevokeFamily(Guid familyId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_refresh_token_revoke_family(@FamilyId)", new { FamilyId = familyId });
        }

        public void RevokeAllForUser(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_refresh_token_revoke_all_for_user(@UserId)", new { UserId = userId });
        }

        public int Prune(DateTime now, DateTime spentBefore)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>(
                "SELECT public.fn_refresh_token_prune(@Now, @SpentBefore)",
                new { Now = Utc(now), SpentBefore = Utc(spentBefore) });
        }
    }
}
