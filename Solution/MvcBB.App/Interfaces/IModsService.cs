using MvcBB.Shared.Models.Mod;

namespace MvcBB.App.Interfaces
{
    public interface IModsService
    {
        /// <summary>Full list for the admin Mods page.</summary>
        Task<IEnumerable<ModRecord>> GetAllAsync();

        Task SetEnabledAsync(string id, bool enabled);

        /// <summary>
        /// The currently-enabled mod Id set, cached locally with a short TTL -
        /// used by both the soft-disable filter and nav-item rendering so most
        /// requests never need a round trip to the API.
        /// </summary>
        Task<HashSet<string>> GetEnabledModIdsAsync();
    }
}
