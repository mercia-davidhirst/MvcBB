using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.Services
{
    public interface IDisplaySettingsService
    {
        DisplaySettings GetDisplaySettings();
        void UpdateDisplaySettings(DisplaySettings settings);
    }

    /// <summary>
    /// Thin wrapper over IDisplaySettingsRepository - previously held its own
    /// state in a static field (seeded once from appsettings.json), which
    /// meant admin changes never survived an app restart and never applied
    /// to the other side of a multi-instance deployment. Now every call
    /// reads/writes the single DisplaySettings row through the data layer.
    /// </summary>
    public class DisplaySettingsService : IDisplaySettingsService
    {
        private readonly IDisplaySettingsRepository _repository;

        public DisplaySettingsService(IDisplaySettingsRepository repository)
        {
            _repository = repository;
        }

        public DisplaySettings GetDisplaySettings() => _repository.Get();

        public void UpdateDisplaySettings(DisplaySettings settings)
        {
            settings.ThreadsPerPage = Math.Clamp(settings.ThreadsPerPage, 1, 100);

            // DateFormat/TimeFormat are a fixed dropdown on the admin page,
            // not free text - a request carrying anything else (e.g. a
            // malformed value sent directly against the API) falls back to
            // whatever's already persisted rather than being written through,
            // since every page on the site calls string.Format with these and
            // an invalid .NET format string would throw a FormatException
            // site-wide.
            if (!DateFormatOptions.IsValidDateFormat(settings.DateFormat) || !DateFormatOptions.IsValidTimeFormat(settings.TimeFormat))
            {
                var current = _repository.Get();
                if (!DateFormatOptions.IsValidDateFormat(settings.DateFormat))
                {
                    settings.DateFormat = current.DateFormat;
                }
                if (!DateFormatOptions.IsValidTimeFormat(settings.TimeFormat))
                {
                    settings.TimeFormat = current.TimeFormat;
                }
            }

            _repository.Update(settings);
        }
    }
}
