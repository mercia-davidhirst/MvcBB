using Microsoft.AspNetCore.Http;

namespace MvcBB.App.Interfaces
{
    /// <summary>
    /// Validates and saves an uploaded image under wwwroot/images/&lt;subfolder&gt;,
    /// returning its public relative path (e.g. "/images/logos/{guid}.png"). Shared
    /// by every feature that lets an admin or user upload an image - smilies, the
    /// site logo, and (in future) avatars/attachments - so the same file-type and
    /// size rules apply everywhere.
    /// </summary>
    public interface IImageUploadService
    {
        Task<string> SaveImageAsync(IFormFile file, string subfolder);

        /// <summary>
        /// Deletes a previously-saved image given its public relative path (as
        /// returned by <see cref="SaveImageAsync"/>). Safe to call with null/empty
        /// or a path that no longer exists on disk.
        /// </summary>
        void DeleteImage(string? relativePath);
    }
}
