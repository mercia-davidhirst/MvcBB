namespace MvcBB.ModKit
{
    // Slot contexts carry ids and display names only - never core's full models
    // (emails, deleted-post content, moderator-only fields), so a mod can't leak
    // those by accident and core can change its models without breaking mods.
    //
    // Compatibility rule: these are public ModKit types, so fields may only ever
    // be added, never renamed or removed. They're classes with init-only
    // properties rather than positional records so adding one stays binary
    // compatible.

    /// <summary>
    /// Everything slots on the current page will be asked about, so a mod can
    /// load data for all of it in one call (see <see cref="SlotBatch{TKey, TValue}"/>).
    /// Built once per page by core, and shared by every context on that page.
    /// </summary>
    public sealed class SlotPage
    {
        public static readonly SlotPage Empty = new();

        /// <summary>Distinct ids of every member shown in a slot on this page.</summary>
        public IReadOnlyCollection<string> UserIds { get; init; } = Array.Empty<string>();

        /// <summary>Ids of every post shown in a slot on this page.</summary>
        public IReadOnlyCollection<int> PostIds { get; init; } = Array.Empty<int>();
    }

    /// <summary>What every slot context has.</summary>
    public abstract class SlotContext
    {
        /// <summary>The signed-in viewer's user id, or null for a guest.</summary>
        public string? ViewerUserId { get; init; }

        /// <summary>Every user and post id on the page, for batch lookups.</summary>
        public SlotPage Page { get; init; } = SlotPage.Empty;

        public bool ViewerIsSignedIn => !string.IsNullOrEmpty(ViewerUserId);
    }

    /// <summary>For slots that aren't about one member or post (home page, layout).</summary>
    public sealed class PageSlotContext : SlotContext
    {
    }

    /// <summary>For slots about one member (profile page).</summary>
    public sealed class UserSlotContext : SlotContext
    {
        public string UserId { get; init; } = string.Empty;

        public string Username { get; init; } = string.Empty;

        /// <summary>True when the viewer is looking at their own profile.</summary>
        public bool IsViewerOwnProfile => ViewerIsSignedIn && string.Equals(ViewerUserId, UserId, StringComparison.Ordinal);
    }

    /// <summary>For slots in one post on a thread page.</summary>
    public sealed class PostSlotContext : SlotContext
    {
        public int PostId { get; init; }

        public int ThreadId { get; init; }

        public int BoardId { get; init; }

        public string AuthorUserId { get; init; } = string.Empty;

        public string AuthorUsername { get; init; } = string.Empty;
    }
}
