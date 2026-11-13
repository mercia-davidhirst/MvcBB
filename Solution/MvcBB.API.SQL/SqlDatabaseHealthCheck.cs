using Dapper;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.SQL
{
    public class SqlDatabaseHealthCheck : IDatabaseHealthCheck
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlDatabaseHealthCheck(ISqlConnectionFactory connectionFactory)
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
