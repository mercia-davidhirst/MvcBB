using MvcBB.Shared.Models.Settings;

namespace MvcBB.Shared.Interfaces
{
    /// <summary>
    /// Site-wide display settings - a single configurable row, seeded once
    /// by the installer's seed data, never inserted/deleted at runtime.
    /// </summary>
    public interface IDisplaySettingsRepository
    {
        DisplaySettings Get();
        void Update(DisplaySettings settings);
    }
}
