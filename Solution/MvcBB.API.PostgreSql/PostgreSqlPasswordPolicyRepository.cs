using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed password policy repository. public.password_policy's
    /// snake_case columns map onto the PasswordPolicy model automatically
    /// via Dapper.DefaultTypeMap.MatchNamesWithUnderscores (see
    /// ServiceCollectionExtensions), so no translation row class is needed.
    /// </summary>
    public class PostgreSqlPasswordPolicyRepository : IPasswordPolicyRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlPasswordPolicyRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PasswordPolicy Get()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<PasswordPolicy>("SELECT * FROM public.fn_password_policy_get()");
        }

        public void Update(PasswordPolicy policy)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_password_policy_update(@MinimumLength, @RequireUppercase, @RequireLowercase, @RequireDigit, @RequireSpecialCharacter)",
                new
                {
                    policy.MinimumLength,
                    policy.RequireUppercase,
                    policy.RequireLowercase,
                    policy.RequireDigit,
                    policy.RequireSpecialCharacter
                });
        }
    }
}
