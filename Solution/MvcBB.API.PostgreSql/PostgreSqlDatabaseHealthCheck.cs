using Dapper;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlDatabaseHealthCheck : IDatabaseHealthCheck
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlDatabaseHealthCheck(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool IsHealthy()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                connection.ExecuteScalar<int>("SELECT 1");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
