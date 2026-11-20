using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Mod;

namespace MvcBB.API.SQL
{
    public class SqlModRepository : IModRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlModRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<ModRecord> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<ModRecord>("dbo.usp_Mod_GetAll", commandType: CommandType.StoredProcedure).ToList();
        }

        public ModRecord? GetById(string id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<ModRecord>("dbo.usp_Mod_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public void Upsert(string id, string name, string version, string minCoreVersion, string? maxCoreVersion, string status)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Mod_Upsert", new
            {
                Id = id,
                Name = name,
                Version = version,
                MinCoreVersion = minCoreVersion,
                MaxCoreVersion = maxCoreVersion,
                Status = status
            }, commandType: CommandType.StoredProcedure);
        }

        public void SetInstalledVersion(string id, string version)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Mod_SetInstalledVersion", new { Id = id, Version = version }, commandType: CommandType.StoredProcedure);
        }

        public void SetEnabled(string id, bool enabled)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Mod_SetEnabled", new { Id = id, IsEnabled = enabled }, commandType: CommandType.StoredProcedure);
        }
    }
}
