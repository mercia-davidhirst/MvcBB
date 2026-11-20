namespace MvcBB.ModKit
{
    /// <summary>
    /// Tags a mod's controller (either tier) with the mod Id it belongs to, so
    /// the soft-disable action filter can look up whether that mod is enabled
    /// before letting the action run. Core's own controllers never carry this
    /// attribute and are never touched by the filter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModAttribute : Attribute
    {
        public string ModId { get; }

        public ModAttribute(string modId)
        {
            ModId = modId;
        }
    }
}
