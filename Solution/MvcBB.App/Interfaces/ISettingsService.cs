using MvcBB.Shared.Models.Settings;

namespace MvcBB.App.Interfaces
{
    public interface ISettingsService
    {
        Task<DisplaySettings> GetDisplaySettingsAsync();
        Task UpdateDisplaySettingsAsync(DisplaySettings settings);
    }
}
