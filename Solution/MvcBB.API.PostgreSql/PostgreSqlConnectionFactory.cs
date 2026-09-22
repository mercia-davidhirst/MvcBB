using System.Data;
using Npgsql;

namespace MvcBB.API.PostgreSql
{
    public interface IPostgreSqlConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class PostgreSqlConnectionFactory : IPostgreSqlConnectionFactory
    {
        private readonly string _connectionString;

        public PostgreSqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
    }
}
