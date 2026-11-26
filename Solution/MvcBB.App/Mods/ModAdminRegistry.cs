using MvcBB.App.Interfaces;
using MvcBB.ModKit;

namespace MvcBB.App.Mods
{
    /// <summary>
    /// Every administration page the discovered App-side mods add (see
    /// <see cref="IAppMod.GetAdminItems"/>), read and checked once at startup.
    /// Core shows them in all three admin entry points: a card on the Admin
    /// Dashboard, an item in the navbar's Admin dropdown and a link in the
    /// admin sidebar, for enabled mods only.
    ///
    /// A mod that throws from GetAdminItems, or lists an item without a
    /// controller or action, is logged and that item is dropped; the rest of
    /// the mod still works.
    /// </summary>
    public class ModAdminRegistry
    {
        public sealed record Entry(string ModId, ModAdminItem Item);

        private static readonly object ItemsKey = new();

        private readonly IReadOnlyList<Entry> _entries;

        private ModAdminRegistry(IReadOnlyList<Entry> entries)
        {
            _entries = entries;
        }

        /// <summary>Every item, whether or not its mod is enabled, in a stable order (by text, then mod Id).</summary>
        public IReadOnlyList<Entry> All => _entries;

        /// <summary>
        /// The items of currently enabled mods. Reads the enabled set (cached
        /// by <see cref="IModsService"/>) at most once per request, since the
        /// layout, the sidebar and the Dashboard all ask.
        /// </summary>
        public async Task<IReadOnlyList<Entry>> GetEnabledAsync(HttpContext httpContext, IModsService modsService)
        {
            if (_entries.Count == 0)
                return _entries;

            if (httpContext.Items.TryGetValue(ItemsKey, out var cached) && cached is IReadOnlyList<Entry> list)
                return list;

            IReadOnlyList<Entry> enabled;
            try
            {
                var enabledIds = await modsService.GetEnabledModIdsAsync();
                enabled = _entries.Where(e => enabledIds.Contains(e.ModId)).ToList();
            }
            catch
            {
                // Never break the layout over a mod link.
                enabled = Array.Empty<Entry>();
            }

            httpContext.Items[ItemsKey] = enabled;
            return enabled;
        }

        public static ModAdminRegistry Build(IEnumerable<IAppMod> appMods, ILogger logger)
        {
            var entries = new List<Entry>();
            foreach (var mod in appMods)
            {
                List<ModAdminItem> items;
                try
                {
                    items = (mod.GetAdminItems() ?? Enumerable.Empty<ModAdminItem>()).ToList();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Mod admin pages: mod \"{ModId}\" threw from GetAdminItems, so it adds no admin pages.", mod.Id);
                    continue;
                }

                foreach (var item in items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Text)
                        || string.IsNullOrWhiteSpace(item.Controller) || string.IsNullOrWhiteSpace(item.Action))
                    {
                        logger.LogError("Mod admin pages: mod \"{ModId}\" listed an admin page without text, controller or action; it was dropped.", mod.Id);
                        continue;
                    }

                    entries.Add(new Entry(mod.Id, item));
                    logger.LogInformation("Mod admin pages: mod \"{ModId}\" adds \"{Text}\" ({Controller}/{Action}).",
                        mod.Id, item.Text, item.Controller, item.Action);
                }
            }

            return new ModAdminRegistry(entries
                .OrderBy(e => e.Item.Text, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.ModId, StringComparer.Ordinal)
                .ToList());
        }
    }
}
