namespace MvcBB.Shared.Models.Report
{
    /// <summary>
    /// Cleans up the page address attached to a site issue report. Only a
    /// site-relative path ("/Threads/Details/5?page=2") is kept: absolute
    /// URLs, protocol-relative ("//host") and backslash tricks are dropped, so
    /// the link shown to moderators on the report can only ever point back
    /// into this site.
    /// </summary>
    public static class ReportPageUrl
    {
        public const int MaxLength = 500;

        /// <returns>The trimmed path, or null if it's empty or not a safe site-relative path.</returns>
        public static string? Normalize(string? pageUrl)
        {
            if (string.IsNullOrWhiteSpace(pageUrl))
            {
                return null;
            }

            var value = pageUrl.Trim();
            if (value.Length > MaxLength)
            {
                return null;
            }

            // Must start with a single "/" - "//evil.example" and "/\evil.example"
            // are treated as absolute by browsers.
            if (value[0] != '/' || (value.Length > 1 && (value[1] == '/' || value[1] == '\\')))
            {
                return null;
            }

            if (value.Any(c => char.IsControl(c)))
            {
                return null;
            }

            return value;
        }
    }
}
