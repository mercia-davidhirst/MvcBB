using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MvcBB.ModKit
{
    /// <summary>
    /// Entry point a mod's App-side assembly implements so MvcBB.App can
    /// discover it and register its controllers/views/nav contributions.
    /// Deliberately "dumb" compared to IApiMod - no version/compatibility
    /// metadata, no install step. The Mods registry (Name/Version/compat/
    /// enabled) is owned entirely by the matching IApiMod; this side just
    /// asks the API whether its Id is currently enabled before rendering
    /// anything. One implementation per mod assembly, public parameterless
    /// constructor.
    /// </summary>
    public interface IAppMod
    {
        /// <summary>Must match the Id of this mod's IApiMod counterpart.</summary>
        string Id { get; }

        string Name { get; }

        /// <summary>Registers this mod's own App-side services into the host's container.</summary>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Nav links this mod wants to contribute. The host filters these by
        /// the currently-enabled mod set at render time - a disabled mod's
        /// nav items are simply skipped.
        /// </summary>
        IEnumerable<ModNavItem> GetNavItems();
    }
}
