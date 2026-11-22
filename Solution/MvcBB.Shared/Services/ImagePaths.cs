using System.Text.RegularExpressions;

namespace MvcBB.Shared.Services
{
    /// <summary>
    /// The only shapes of image URL the core site stores for post images and
    /// avatars. The App deletes the file at a stored path when an image is
    /// replaced or removed, so a client-chosen value such as
    /// "/../appsettings.json" or "C:\..." must never be accepted or stored.
    /// Mirrors the Gallery mod's GalleryImagePath.
    /// </summary>
    public static class ImagePaths
    {
        private const string Guid = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
        private const string Ext = @"\.(png|jpg|jpeg|gif)";

        /// <summary>A post image saved by ImageUploadService: /images/posts/&lt;guid&gt;.&lt;ext&gt;</summary>
        public const string PostImagePattern = @"^/images/posts/" + Guid + Ext + "$";

        /// <summary>
        /// An avatar: either one saved by ImageUploadService
        /// (/images/avatars/&lt;guid&gt;.&lt;ext&gt;) or a migrated legacy avatar from
        /// the shared avatar gallery (/images/avatars/avatar-12.jpg and the like).
        /// One path segment, letters/digits/'-'/'_' only, so it can never leave
        /// the avatars folder.
        /// </summary>
        public const string AvatarPattern = @"^/images/avatars/[A-Za-z0-9_-]{1,100}" + Ext + "$";

        /// <summary>
        /// Files the App itself created on upload (GUID-named) - the only ones it
        /// may ever delete. Legacy gallery avatars are shared by several members
        /// and must not be deleted when one member changes theirs.
        /// </summary>
        public static readonly Regex UploadedFilePattern = new(
            @"^/images/(?<folder>posts|avatars|logos)/(?<file>" + Guid + Ext + ")$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
