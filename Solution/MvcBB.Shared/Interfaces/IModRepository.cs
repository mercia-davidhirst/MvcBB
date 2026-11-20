using MvcBB.Shared.Models.Mod;

namespace MvcBB.Shared.Interfaces
{
    public interface IModRepository
    {
        IReadOnlyList<ModRecord> GetAll();
        ModRecord? GetById(string id);

        /// <summary>Discovery/metadata only - preserves the existing row's IsEnabled and InstalledVersion.</summary>
        void Upsert(string id, string name, string version, string minCoreVersion, string? maxCoreVersion, string status);

        void SetInstalledVersion(string id, string version);
        void SetEnabled(string id, bool enabled);
    }
}
