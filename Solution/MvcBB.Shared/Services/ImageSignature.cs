namespace MvcBB.Shared.Services
{
    /// <summary>
    /// Works out what kind of image a file really is from its first few bytes
    /// (its "magic number"), instead of trusting the file name or the content
    /// type the browser sent, both of which the uploader controls.
    /// </summary>
    public static class ImageSignature
    {
        /// <summary>
        /// Returns ".png", ".jpg" or ".gif" for a real PNG, JPEG or GIF image,
        /// or null for anything else. Leaves the stream at position 0 when it
        /// can seek.
        /// </summary>
        public static async Task<string?> DetectExtensionAsync(Stream stream)
        {
            var header = new byte[8];
            var read = 0;
            while (read < header.Length)
            {
                var n = await stream.ReadAsync(header.AsMemory(read, header.Length - read));
                if (n == 0)
                {
                    break;
                }
                read += n;
            }

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            return Detect(header.AsSpan(0, read));
        }

        public static string? Detect(ReadOnlySpan<byte> header)
        {
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (header.Length >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            {
                return ".png";
            }

            // JPEG: FF D8 FF
            if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return ".jpg";
            }

            // GIF: "GIF87a" or "GIF89a"
            if (header.Length >= 6 &&
                header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F' &&
                header[3] == (byte)'8' && (header[4] == (byte)'7' || header[4] == (byte)'9') && header[5] == (byte)'a')
            {
                return ".gif";
            }

            return null;
        }
    }
}
