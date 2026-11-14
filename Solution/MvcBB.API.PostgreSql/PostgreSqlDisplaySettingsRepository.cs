using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed display settings repository. public.display_settings'
    /// snake_case columns map onto the DisplaySettings model automatically
    /// via Dapper.DefaultTypeMap.MatchNamesWithUnderscores (see
    /// ServiceCollectionExtensions), so no translation row class is needed.
    /// </summary>
    public class PostgreSqlDisplaySettingsRepository : IDisplaySettingsRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlDisplaySettingsRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public DisplaySettings Get()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<DisplaySettings>("SELECT * FROM public.fn_display_settings_get()");
        }

        public void Update(DisplaySettings settings)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_display_settings_update(@ThreadsPerPage, @SiteTitle, @WelcomeMessage, @SiteName, @LogoPath, @FooterDisclaimer, @RegistrationEnabled, @DateFormat, @TimeFormat)",
                new
                {
                    settings.ThreadsPerPage,
                    settings.SiteTitle,
                    settings.WelcomeMessage,
                    settings.SiteName,
                    settings.LogoPath,
                    settings.FooterDisclaimer,
                    settings.RegistrationEnabled,
                    settings.DateFormat,
                    settings.TimeFormat
                });
        }
    }
}
