using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Mod;

namespace MvcBB.API.InMemory
{
    public class InMemoryModRepository : IModRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryModRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<ModRecord> GetAll() => _store.Mods.OrderBy(m => m.Name).ToList();

        public ModRecord? GetById(string id) => _store.Mods.FirstOrDefault(m => m.Id == id);

        public void Upsert(string id, string name, string version, string minCoreVersion, string? maxCoreVersion, string status)
        {
            var existing = _store.Mods.FirstOrDefault(m => m.Id == id);
            if (existing != null)
            {
                existing.Name = name;
                existing.Version = version;
                existing.MinCoreVersion = minCoreVersion;
                existing.MaxCoreVersion = maxCoreVersion;
                existing.Status = status;
            }
            else
            {
                _store.Mods.Add(new ModRecord
                {
                    Id = id,
                    Name = name,
                    Version = version,
                    MinCoreVersion = minCoreVersion,
                    MaxCoreVersion = maxCoreVersion,
                    InstalledVersion = null,
                    IsEnabled = false,
                    Status = status,
                    DiscoveredAt = DateTime.UtcNow
                });
            }
        }

        public void SetInstalledVersion(string id, string version)
        {
            var mod = _store.Mods.FirstOrDefault(m => m.Id == id);
            if (mod != null)
                mod.InstalledVersion = version;
        }

        public void SetEnabled(string id, bool enabled)
        {
            var mod = _store.Mods.FirstOrDefault(m => m.Id == id);
            if (mod != null)
                mod.IsEnabled = enabled;
        }
    }
}
