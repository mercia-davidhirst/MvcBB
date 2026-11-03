using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.Services
{
    public interface IDisplaySettingsService
    {
        DisplaySettings GetDisplaySettings();
        void UpdateDisplaySettings(DisplaySettings settings);
    }

    public class DisplaySettingsService : IDisplaySettingsService
    {
        private static DisplaySettings _settings = new();
        private readonly IConfiguration _configuration;
        private static bool _initialized;

        public DisplaySettingsService(IConfiguration configuration)
        {
            _configuration = configuration;
            if (!_initialized)
            {
                _settings.ThreadsPerPage = _configuration.GetValue("Display:ThreadsPerPage", 20);
                _settings.SiteTitle = _configuration.GetValue("Display:SiteTitle", _settings.SiteTitle) ?? _settings.SiteTitle;
                _settings.WelcomeMessage = _configuration.GetValue("Display:WelcomeMessage", _settings.WelcomeMessage) ?? _settings.WelcomeMessage;
                _settings.SiteName = _configuration.GetValue("Display:SiteName", _settings.SiteName) ?? _settings.SiteName;
                _settings.LogoPath = _configuration.GetValue<string?>("Display:LogoPath", null);
                _initialized = true;
            }
        }

        public DisplaySettings GetDisplaySettings()
        {
            return new DisplaySettings
            {
                ThreadsPerPage = _settings.ThreadsPerPage,
                SiteTitle = _settings.SiteTitle,
                WelcomeMessage = _settings.WelcomeMessage,
                SiteName = _settings.SiteName,
                LogoPath = _settings.LogoPath
            };
        }

        public void UpdateDisplaySettings(DisplaySettings settings)
        {
            _settings.ThreadsPerPage = Math.Clamp(settings.ThreadsPerPage, 1, 100);
            _settings.SiteTitle = settings.SiteTitle;
            _settings.WelcomeMessage = settings.WelcomeMessage;
            _settings.SiteName = settings.SiteName;
            _settings.LogoPath = settings.LogoPath;
        }
    }
}
