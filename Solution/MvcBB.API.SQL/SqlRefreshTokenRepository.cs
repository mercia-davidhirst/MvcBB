using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed "Remember me" refresh token repository, calling the
    /// usp_RefreshToken_* stored procedures in Database/SQL/Stored Procedures.
    /// </summary>
    public class SqlRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlRefreshTokenRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public RefreshToken Add(RefreshToken token)
        {
            var parameters = new DynamicParameters();
            parameters.Add("UserId", token.UserId);
            parameters.Add("TokenHash", token.TokenHash);
            parameters.Add("FamilyId", token.FamilyId);
            parameters.Add("SessionStamp", token.SessionStamp);
            parameters.Add("CreatedAt", token.CreatedAt, DbType.DateTime2);
            parameters.Add("ExpiresAt", token.ExpiresAt, DbType.DateTime2);
            parameters.Add("CreatedIp", token.CreatedIp);

            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<RefreshToken>("dbo.usp_RefreshToken_Insert", parameters, commandType: CommandType.StoredProcedure);
        }

        public RefreshToken? GetByTokenHash(string tokenHash)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<RefreshToken>("dbo.usp_RefreshToken_GetByTokenHash", new { TokenHash = tokenHash }, commandType: CommandType.StoredProcedure);
        }

        public bool MarkUsed(int id, DateTime usedAt)
        {
            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("UsedAt", usedAt, DbType.DateTime2);

            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_RefreshToken_MarkUsed", parameters, commandType: CommandType.StoredProcedure) > 0;
        }

        public void RevokeFamily(Guid familyId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_RefreshToken_RevokeFamily", new { FamilyId = familyId }, commandType: CommandType.StoredProcedure);
        }

        public void RevokeAllForUser(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_RefreshToken_RevokeAllForUser", new { UserId = userId }, commandType: CommandType.StoredProcedure);
        }

        public int Prune(DateTime now, DateTime spentBefore)
        {
            var parameters = new DynamicParameters();
            parameters.Add("Now", now, DbType.DateTime2);
            parameters.Add("SpentBefore", spentBefore, DbType.DateTime2);

            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_RefreshToken_Prune", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
