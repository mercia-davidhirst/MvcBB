using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Mod;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlModRepository : IModRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlModRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<ModRecord> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<ModRecord>("SELECT * FROM public.fn_mod_get_all()").ToList();
        }

        public ModRecord? GetById(string id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<ModRecord>("SELECT * FROM public.fn_mod_get_by_id(@Id)", new { Id = id });
        }

        public void Upsert(string id, string name, string version, string minCoreVersion, string? maxCoreVersion, string status)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_mod_upsert(@Id, @Name, @Version, @MinCoreVersion, @MaxCoreVersion, @Status)", new
            {
                Id = id,
                Name = name,
                Version = version,
                MinCoreVersion = minCoreVersion,
                MaxCoreVersion = maxCoreVersion,
                Status = status
            });
        }

        public void SetInstalledVersion(string id, string version)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_mod_set_installed_version(@Id, @Version)", new { Id = id, Version = version });
        }

        public void SetEnabled(string id, bool enabled)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_mod_set_enabled(@Id, @IsEnabled)", new { Id = id, IsEnabled = enabled });
        }
    }
}
