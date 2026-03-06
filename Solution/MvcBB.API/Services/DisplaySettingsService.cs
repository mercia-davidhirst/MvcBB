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
                _initialized = true;
            }
        }

        public DisplaySettings GetDisplaySettings()
        {
            return new DisplaySettings { ThreadsPerPage = _settings.ThreadsPerPage };
        }

        public void UpdateDisplaySettings(DisplaySettings settings)
        {
            _settings.ThreadsPerPage = Math.Clamp(settings.ThreadsPerPage, 1, 100);
        }
    }
}
