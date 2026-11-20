namespace MvcBB.Shared.Models.Mod
{
    /// <summary>
    /// Backing model for dbo.Mods / public.mods - a discovered mod assembly's
    /// metadata plus its admin-controlled enabled state. See IModRepository.
    /// </summary>
    public class ModRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string MinCoreVersion { get; set; } = string.Empty;
        public string? MaxCoreVersion { get; set; }
        public string? InstalledVersion { get; set; }
        public bool IsEnabled { get; set; }
        public string Status { get; set; } = ModStatus.Compatible;
        public DateTime DiscoveredAt { get; set; }
    }

    public static class ModStatus
    {
        public const string Compatible = "Compatible";
        public const string Incompatible = "Incompatible";
    }
}
