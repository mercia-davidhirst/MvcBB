using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using MvcBB.ModKit;

namespace MvcBB.App.Mods
{
    /// <summary>
    /// Every view slot contribution from the discovered App-side mods, checked
    /// and sorted once at startup. Read by <see cref="TagHelpers.ModSlotTagHelper"/>
    /// on every slot render, so lookups are a single dictionary read.
    ///
    /// A broken contribution never stops the site from starting: it's logged
    /// and dropped, and the rest of that mod (its pages, nav links and other
    /// contributions) still works. See docs/ModKit-ViewSlots.md.
    /// </summary>
    public class ModSlotRegistry
    {
        /// <summary>How a slot is laid out, and the context type it passes.</summary>
        public sealed record SlotDefinition(string Name, Type ContextType, SlotWrapper Wrapper);

        public enum SlotWrapper
        {
            /// <summary>Each contribution wrapped in a &lt;div&gt;.</summary>
            Block,
            /// <summary>No wrapper at all (for &lt;head&gt; content).</summary>
            None
        }

        public sealed record Entry(string ModId, Type ViewComponentType, int Order);

        /// <summary>The slots core views expose. Keep in step with <see cref="CoreSlots"/>.</summary>
        public static readonly IReadOnlyDictionary<string, SlotDefinition> KnownSlots =
            new[]
            {
                new SlotDefinition(CoreSlots.PostAuthor, typeof(PostSlotContext), SlotWrapper.Block),
                new SlotDefinition(CoreSlots.PostFooter, typeof(PostSlotContext), SlotWrapper.Block),
                new SlotDefinition(CoreSlots.ProfileSidebar, typeof(UserSlotContext), SlotWrapper.Block),
                new SlotDefinition(CoreSlots.HomeSidebar, typeof(PageSlotContext), SlotWrapper.Block),
                new SlotDefinition(CoreSlots.HomeLeft, typeof(PageSlotContext), SlotWrapper.Block),
                new SlotDefinition(CoreSlots.HomeCenterTop, typeof(PageSlotContext), SlotWrapper.Block),
                new SlotDefinition(CoreSlots.HomeCenter, typeof(PageSlotContext), SlotWrapper.Block),
                new SlotDefinition(CoreSlots.HomeRight, typeof(PageSlotContext), SlotWrapper.Block),
                new SlotDefinition(CoreSlots.LayoutHead, typeof(PageSlotContext), SlotWrapper.None),
                new SlotDefinition(CoreSlots.LayoutNavbar, typeof(PageSlotContext), SlotWrapper.None),
                new SlotDefinition(CoreSlots.LayoutToasts, typeof(PageSlotContext), SlotWrapper.None),
            }.ToDictionary(s => s.Name, StringComparer.Ordinal);

        private static readonly IReadOnlyList<Entry> NoEntries = Array.Empty<Entry>();

        private readonly Dictionary<string, IReadOnlyList<Entry>> _bySlot;

        private ModSlotRegistry(Dictionary<string, IReadOnlyList<Entry>> bySlot)
        {
            _bySlot = bySlot;
        }

        /// <summary>True when at least one mod contributes to any slot.</summary>
        public bool HasAny => _bySlot.Count > 0;

        /// <summary>Contributions for a slot, in render order. Empty when none.</summary>
        public IReadOnlyList<Entry> GetEntries(string slot) =>
            _bySlot.TryGetValue(slot, out var entries) ? entries : NoEntries;

        public static ModSlotRegistry Build(IEnumerable<IAppMod> appMods, ILogger logger)
        {
            var collected = new Dictionary<string, List<Entry>>(StringComparer.Ordinal);

            foreach (var mod in appMods)
            {
                List<ModSlotContribution> contributions;
                try
                {
                    contributions = (mod.GetSlotContributions() ?? Enumerable.Empty<ModSlotContribution>()).ToList();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Mod slots: mod \"{ModId}\" threw from GetSlotContributions, so it contributes to no slots.", mod.Id);
                    continue;
                }

                foreach (var contribution in contributions)
                {
                    if (contribution == null)
                        continue;

                    if (!KnownSlots.TryGetValue(contribution.Slot, out var slot))
                    {
                        logger.LogWarning("Mod slots: mod \"{ModId}\" named slot \"{Slot}\", which this version of core doesn't have. Ignored.",
                            mod.Id, contribution.Slot);
                        continue;
                    }

                    var problem = Validate(mod, contribution.ViewComponentType, slot.ContextType);
                    if (problem != null)
                    {
                        logger.LogError("Mod slots: dropped {ViewComponent} from mod \"{ModId}\" for slot \"{Slot}\": {Problem}",
                            contribution.ViewComponentType.FullName, mod.Id, contribution.Slot, problem);
                        continue;
                    }

                    if (!collected.TryGetValue(slot.Name, out var list))
                        collected[slot.Name] = list = new List<Entry>();
                    list.Add(new Entry(mod.Id, contribution.ViewComponentType, contribution.Order));

                    logger.LogInformation("Mod slots: mod \"{ModId}\" renders {ViewComponent} into \"{Slot}\".",
                        mod.Id, contribution.ViewComponentType.Name, slot.Name);
                }
            }

            var sorted = collected.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyList<Entry>)kv.Value
                    .OrderBy(e => e.Order)
                    .ThenBy(e => e.ModId, StringComparer.Ordinal)
                    .ThenBy(e => e.ViewComponentType.FullName, StringComparer.Ordinal)
                    .ToList(),
                StringComparer.Ordinal);

            return new ModSlotRegistry(sorted);
        }

        private static string? Validate(IAppMod mod, Type type, Type contextType)
        {
            if (type.Assembly != mod.GetType().Assembly)
                return "the view component must live in the mod's own assembly.";

            if (type.IsAbstract || !type.IsClass || !type.IsPublic)
                return "the view component must be a public, non-abstract class.";

            if (!typeof(ViewComponent).IsAssignableFrom(type) && type.GetCustomAttribute<ViewComponentAttribute>() == null)
                return "the type isn't a view component (derive from ViewComponent).";

            var invokeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name is "InvokeAsync" or "Invoke")
                .ToList();
            if (invokeMethods.Count != 1)
                return "it must have exactly one public InvokeAsync or Invoke method.";

            var parameters = invokeMethods[0].GetParameters();
            if (parameters.Length != 1 || parameters[0].Name != "context" || !parameters[0].ParameterType.IsAssignableFrom(contextType))
                return $"its {invokeMethods[0].Name} method must take one parameter named \"context\" that accepts {contextType.Name}.";

            return null;
        }
    }
}
