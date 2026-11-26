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

        /// <summary>
        /// View components this mod renders into core view slots (see
        /// <see cref="CoreSlots"/>). Filtered by the enabled-mod set at render
        /// time, exactly like <see cref="GetNavItems"/>: a disabled mod's
        /// components aren't invoked at all. Read once, at startup.
        /// </summary>
        /// <remarks>
        /// Has a default body (added in ModKit 1.1.0) so mods compiled against
        /// 1.0.0 still load unchanged and simply contribute nothing.
        /// </remarks>
        IEnumerable<ModSlotContribution> GetSlotContributions() => Array.Empty<ModSlotContribution>();

        /// <summary>
        /// Administration pages this mod adds to core's admin entry points (the
        /// Admin Dashboard, the navbar's Admin dropdown and the admin sidebar).
        /// Shown to Administrators only, and filtered by the enabled-mod set
        /// like <see cref="GetNavItems"/>. The mod's own controller still has
        /// to restrict the page to Administrators.
        /// </summary>
        /// <remarks>
        /// Has a default body (added in ModKit 1.3.0) so mods compiled against
        /// older versions still load unchanged and simply add no admin pages.
        /// </remarks>
        IEnumerable<ModAdminItem> GetAdminItems() => Array.Empty<ModAdminItem>();
    }
}
