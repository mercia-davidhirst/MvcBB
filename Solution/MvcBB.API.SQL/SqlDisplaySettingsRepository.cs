using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed display settings repository. dbo.DisplaySettings'
    /// columns match the DisplaySettings model 1:1, so no translation row
    /// class is needed here.
    /// </summary>
    public class SqlDisplaySettingsRepository : IDisplaySettingsRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlDisplaySettingsRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public DisplaySettings Get()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<DisplaySettings>("dbo.usp_DisplaySettings_Get", commandType: CommandType.StoredProcedure);
        }

        public void Update(DisplaySettings settings)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_DisplaySettings_Update", new
            {
                settings.ThreadsPerPage,
                settings.SiteTitle,
                settings.WelcomeMessage,
                settings.SiteName,
                settings.LogoPath,
                settings.FooterDisclaimer,
                settings.RegistrationEnabled
            }, commandType: CommandType.StoredProcedure);
        }
    }
}
