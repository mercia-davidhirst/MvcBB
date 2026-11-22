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
        public const string HomeSidebar = "home.sidebar";

        /// <summary>
        /// Inside the &lt;head&gt; of the main layout, after core's stylesheets, on
        /// every page that uses it. For &lt;link rel="stylesheet"&gt; tags only (no
        /// wrapper element is written here). Context: <see cref="PageSlotContext"/>.
        /// </summary>
        public const string LayoutHead = "layout.head";
    }
}
