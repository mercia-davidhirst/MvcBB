using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed password policy repository. dbo.PasswordPolicy's
    /// columns match the PasswordPolicy model 1:1, so no translation row
    /// class is needed here.
    /// </summary>
    public class SqlPasswordPolicyRepository : IPasswordPolicyRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlPasswordPolicyRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PasswordPolicy Get()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<PasswordPolicy>("dbo.usp_PasswordPolicy_Get", commandType: CommandType.StoredProcedure);
        }

        public void Update(PasswordPolicy policy)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_PasswordPolicy_Update", new
            {
                policy.MinimumLength,
                policy.RequireUppercase,
                policy.RequireLowercase,
                policy.RequireDigit,
                policy.RequireSpecialCharacter
            }, commandType: CommandType.StoredProcedure);
        }
    }
}
