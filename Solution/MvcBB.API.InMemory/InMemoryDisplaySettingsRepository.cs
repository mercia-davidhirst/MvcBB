using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.InMemory
{
    public class InMemoryDisplaySettingsRepository : IDisplaySettingsRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryDisplaySettingsRepository(InMemoryStore store)
        {
            _store = store;
        }

        public DisplaySettings Get() => _store.DisplaySettings;

        public void Update(DisplaySettings settings) => _store.DisplaySettings = settings;
    }
}
