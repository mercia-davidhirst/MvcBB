using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MvcBB.ModKit
{
    /// <summary>
    /// Entry point a mod's API-side assembly implements so MvcBB.API can
    /// discover it, register its services/controllers, and install its own
    /// database schema. One implementation per mod assembly, with a public
    /// parameterless constructor (the loader instantiates it via reflection
    /// before DI exists).
    /// </summary>
    public interface IApiMod
    {
        /// <summary>Stable key identifying this mod, e.g. "gallery". Never changes across versions - this is the join key between a mod's API-side and App-side assemblies, and the primary key in the Mods registry table.</summary>
        string Id { get; }

        string Name { get; }

        /// <summary>This mod's own version.</summary>
        string Version { get; }

        /// <summary>Lowest MvcBB.ModKit version (core contract version) this mod works against.</summary>
        string MinCoreVersion { get; }

        /// <summary>Highest MvcBB.ModKit version this mod works against, or null for no upper bound.</summary>
        string? MaxCoreVersion { get; }

        /// <summary>Registers this mod's own services (repositories, business logic) into the host's container.</summary>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Installs/upgrades this mod's own database schema. Must be idempotent -
        /// the loader only calls this when the mod's declared Version differs from
        /// the InstalledVersion already recorded in the Mods registry, but a mod
        /// should still guard its own DDL the same way core's table scripts do.
        /// </summary>
        Task InstallAsync(IModInstallContext context);
    }
}
