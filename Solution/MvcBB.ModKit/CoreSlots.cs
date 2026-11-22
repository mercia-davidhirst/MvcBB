namespace MvcBB.ModKit
{
    /// <summary>
    /// Names of the view slots core pages expose (see <see cref="ModSlotContribution"/>).
    /// They're plain strings, so a mod built against a newer ModKit can name a
    /// slot an older core doesn't have yet: the host logs and ignores unknown
    /// names instead of failing.
    /// </summary>
    public static class CoreSlots
    {
        /// <summary>
        /// Thread page, in each post's author block, under the member's name and
        /// rank title. Block, small: about one or two short lines at small text
        /// size (an icon and a few words). Context: <see cref="PostSlotContext"/>.
        /// </summary>
        public const string PostAuthor = "post.author";

        /// <summary>
        /// Thread page, in each post's body, after any attached image and before
        /// the signature. Block, full width, for a short strip rather than a
        /// panel. Context: <see cref="PostSlotContext"/>.
        /// </summary>
        public const string PostFooter = "post.footer";

        /// <summary>
        /// Profile page, in the member's details card, directly under the avatar.
        /// Block, centred, narrow (the left column). Context: <see cref="UserSlotContext"/>.
        /// </summary>
        public const string ProfileSidebar = "profile.sidebar";

        /// <summary>
        /// Home page, left column, directly under the Quick Links card. Block,
        /// narrow. Each contribution should render its own card. Context:
        /// <see cref="PageSlotContext"/>.
        /// </summary>
        /// <remarks>
        /// The first home page slot, kept so existing mods work unchanged. It
        /// still renders exactly where it always has, in the left column under
        /// Quick Links, directly before <see cref="HomeLeft"/>.
        /// New mods should use <see cref="HomeLeft"/>, <see cref="HomeCenter"/>,
        /// <see cref="HomeCenterTop"/> or <see cref="HomeRight"/>.
        /// </remarks>
        public const string HomeSidebar = "home.sidebar";

        /// <summary>
        /// Home page (portal), left column, under the Quick Links card and any
        /// <see cref="HomeSidebar"/> contributions. Block, narrow (about a sixth
        /// of the page on desktop, full width on phones). Each contribution
        /// should render its own card. Context: <see cref="PageSlotContext"/>.
        /// Added with the portal slots in v1.1.0.
        /// </summary>
        public const string HomeLeft = "home.left";

        /// <summary>
        /// Home page (portal), centre column, above the Latest Discussions card.
        /// Block, wide. For something that should be seen first (a featured
        /// item, a banner); each contribution should render its own card.
        /// Context: <see cref="PageSlotContext"/>. Added with the portal slots in v1.1.0.
        /// </summary>
        public const string HomeCenterTop = "home.center.top";

        /// <summary>
        /// Home page (portal), centre column, under the Latest Discussions card.
        /// Block, wide. The main place for a larger widget (a gallery strip, a
        /// leaderboard); each contribution should render its own card.
        /// Context: <see cref="PageSlotContext"/>. Added with the portal slots in v1.1.0.
        /// </summary>
        public const string HomeCenter = "home.center";

        /// <summary>
        /// Home page (portal), right column, under the Statistics and Newest
        /// Members cards. Block, narrow (like <see cref="HomeLeft"/>). Each
        /// contribution should render its own card. Context:
        /// <see cref="PageSlotContext"/>. Added with the portal slots in v1.1.0.
        /// </summary>
        public const string HomeRight = "home.right";

        /// <summary>
        /// Inside the &lt;head&gt; of the main layout, after core's stylesheets, on
        /// every page that uses it. For &lt;link rel="stylesheet"&gt; tags only (no
        /// wrapper element is written here). Context: <see cref="PageSlotContext"/>.
        /// </summary>
        public const string LayoutHead = "layout.head";
    }
}
