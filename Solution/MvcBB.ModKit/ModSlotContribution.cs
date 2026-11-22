namespace MvcBB.ModKit
{
    /// <summary>
    /// One view component a mod renders into a core view slot. Returned from
    /// <see cref="IAppMod.GetSlotContributions"/>.
    /// </summary>
    public sealed class ModSlotContribution
    {
        /// <param name="slot">One of <see cref="CoreSlots"/>.</param>
        /// <param name="viewComponentType">
        /// A ViewComponent subclass in the mod's own assembly, with an
        /// <c>InvokeAsync</c> (or <c>Invoke</c>) method taking one parameter named
        /// <c>context</c> of the slot's context type. Invoked by type, never by
        /// name, so two mods (or a mod and core) using the same class name never
        /// collide.
        /// </param>
        /// <param name="order">Lower renders first. Ties are broken by mod Id, so the order is stable across restarts.</param>
        public ModSlotContribution(string slot, Type viewComponentType, int order = 0)
        {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            ViewComponentType = viewComponentType ?? throw new ArgumentNullException(nameof(viewComponentType));
            Order = order;
        }

        public string Slot { get; }

        public Type ViewComponentType { get; }

        public int Order { get; }
    }
}
