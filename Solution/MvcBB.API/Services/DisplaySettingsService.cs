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
            _repository.Update(settings);
        }
    }
}
