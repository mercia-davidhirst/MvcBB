namespace MvcBB.ModKit
{
    /// <summary>
    /// An administration page a mod adds to core's admin entry points: a card
    /// on the Admin Dashboard, an item in the navbar's Admin dropdown, and a
    /// link in the admin sidebar. Shown to Administrators only, and only while
    /// the mod is enabled. The page itself is an ordinary action on the mod's
    /// own <c>[Mod]</c> controller, which must carry
    /// <c>[Authorize(Roles = "Administrator")]</c> itself: listing it here
    /// grants nothing.
    /// </summary>
    /// <remarks>
    /// A class with init-only properties rather than a positional record, so
    /// fields can be added later without breaking mods compiled against this
    /// version (the same rule as the slot contexts).
    /// </remarks>
    public sealed class ModAdminItem
    {
        public ModAdminItem(string text, string controller, string action, string iconCssClass)
        {
            Text = text;
            Controller = controller;
            Action = action;
            IconCssClass = iconCssClass;
        }

        /// <summary>The link text, e.g. "Calendar".</summary>
        public string Text { get; }

        /// <summary>The mod controller's name, without the "Controller" suffix.</summary>
        public string Controller { get; }

        public string Action { get; }

        /// <summary>A Bootstrap Icons class, e.g. "bi-calendar-event".</summary>
        public string IconCssClass { get; }

        /// <summary>One line for the Dashboard card. Optional.</summary>
        public string? Description { get; init; }

        /// <summary>The Dashboard card's button text. Defaults to "Manage {Text}".</summary>
        public string? ButtonText { get; init; }
    }
}
