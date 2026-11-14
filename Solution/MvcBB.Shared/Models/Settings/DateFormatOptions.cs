namespace MvcBB.Shared.Models.Settings
{
    /// <summary>
    /// The only DateFormat/TimeFormat values the admin Settings page offers
    /// and the server accepts - curated .NET custom date/time format strings
    /// rather than free text, so a malformed value (typed directly against
    /// the API rather than through the dropdown) can never be persisted and
    /// then throw a FormatException everywhere a date is rendered.
    /// </summary>
    public static class DateFormatOptions
    {
        public static readonly IReadOnlyList<(string Value, string Label)> DateFormats = new[]
        {
            ("dd/MM/yyyy", "DD/MM/YYYY"),
            ("MM/dd/yyyy", "MM/DD/YYYY"),
            ("yyyy-MM-dd", "YYYY-MM-DD"),
            ("d MMM yyyy", "D Mon YYYY"),
            ("MMMM d, yyyy", "Month D, YYYY"),
        };

        public static readonly IReadOnlyList<(string Value, string Label)> TimeFormats = new[]
        {
            ("HH:mm", "24-hour"),
            ("h:mm tt", "12-hour"),
        };

        public static bool IsValidDateFormat(string? value) => value != null && DateFormats.Any(f => f.Value == value);
        public static bool IsValidTimeFormat(string? value) => value != null && TimeFormats.Any(f => f.Value == value);
    }
}
