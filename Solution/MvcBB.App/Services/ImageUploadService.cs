using MvcBB.App.Exceptions;
using MvcBB.App.Interfaces;

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

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", subfolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/{subfolder}/{uniqueFileName}";
        }

        public void DeleteImage(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return;
            }

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
