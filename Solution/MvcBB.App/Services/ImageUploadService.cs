using MvcBB.App.Exceptions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Services;

namespace MvcBB.App.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".gif" };
        private static readonly string[] AllowedContentTypes = { "image/png", "image/jpeg", "image/gif" };
        private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

        private readonly IWebHostEnvironment _webHostEnvironment;

        public ImageUploadService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> SaveImageAsync(IFormFile file, string subfolder)
        {
            if (file.Length == 0)
            {
                throw new ServiceException("The selected file is empty.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ServiceException("Image must be smaller than 2 MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                throw new ServiceException("Only PNG, JPG, and GIF images are allowed.");
            }

            // Check the file's actual bytes too - the name and content type are
            // whatever the uploader chose to send. The file is then saved with
            // the extension of what it really is, so it's always served with
            // the matching image content type.
            await using var upload = file.OpenReadStream();
            var detectedExtension = await ImageSignature.DetectExtensionAsync(upload);
            if (detectedExtension == null)
            {
                throw new ServiceException("Only PNG, JPG, and GIF images are allowed.");
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", subfolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{detectedExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await upload.CopyToAsync(fileStream);
            }

            return $"/images/{subfolder}/{uniqueFileName}";
        }

        public void DeleteImage(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return;
            }

            // Only ever delete a file this service itself created
            // (/images/{posts|avatars|logos}/<guid>.<ext>). Anything else - a legacy
            // shared avatar, an external URL, or a crafted value such as
            // "/../appsettings.json" or "C:\\..." - is left alone.
            var match = ImagePaths.UploadedFilePattern.Match(relativePath);
            if (!match.Success)
            {
                return;
            }

            var folder = Path.GetFullPath(Path.Combine(_webHostEnvironment.WebRootPath, "images", match.Groups["folder"].Value.ToLowerInvariant()));
            var filePath = Path.GetFullPath(Path.Combine(folder, match.Groups["file"].Value));

            // Belt and braces: the resolved path must still sit directly inside
            // that images folder.
            if (!string.Equals(Path.GetDirectoryName(filePath), folder, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
