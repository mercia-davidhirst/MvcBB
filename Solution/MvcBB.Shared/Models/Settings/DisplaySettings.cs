using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Settings
{
    public class DisplaySettings
    {
        [Range(1, 100, ErrorMessage = "Threads per page must be between 1 and 100")]
        public int ThreadsPerPage { get; set; } = 20;

        /// <summary>
        /// How many posts a discussion shows per page (added in v1.4.0;
        /// before that every post loaded at once).
        /// </summary>
        [Range(5, 100, ErrorMessage = "Posts per page must be between 5 and 100")]
        [Display(Name = "Posts per page")]
        public int PostsPerPage { get; set; } = 20;

        [Required(ErrorMessage = "Site title is required")]
        [StringLength(100, ErrorMessage = "Site title cannot exceed 100 characters")]
        [Display(Name = "Site Title")]
        public string SiteTitle { get; set; } = "Welcome to MvcBB";

        [Required(ErrorMessage = "Welcome message is required")]
        [StringLength(500, ErrorMessage = "Welcome message cannot exceed 500 characters")]
        [Display(Name = "Welcome Message")]
        public string WelcomeMessage { get; set; } = "A modern bulletin board system for engaging discussions and community building.";

        [Required(ErrorMessage = "Site name is required")]
        [StringLength(50, ErrorMessage = "Site name cannot exceed 50 characters")]
        [Display(Name = "Site Name")]
        public string SiteName { get; set; } = "MvcBB";

        /// <summary>
        /// Public relative path (e.g. "/images/logos/{guid}.png") to the uploaded
        /// site logo shown in the navbar, or null to fall back to text branding.
        /// </summary>
        [Display(Name = "Site Logo")]
        public string? LogoPath { get; set; }

        /// <summary>
        /// Optional centered text shown in the footer (e.g. a disclaimer) -
        /// blank by default, so nothing extra renders until an admin sets one.
        /// </summary>
        [StringLength(500, ErrorMessage = "Footer disclaimer cannot exceed 500 characters")]
        [Display(Name = "Footer Disclaimer")]
        public string? FooterDisclaimer { get; set; }

        /// <summary>
        /// When false, public self-registration is turned off (AccountController.Register
        /// refuses both the form and the POST) - new accounts can then only be made by
        /// an admin via AccountController.CreateUser.
        /// </summary>
        [Display(Name = "Allow New Registrations")]
        public bool RegistrationEnabled { get; set; } = true;

        /// <summary>
        /// A .NET custom date format string, restricted to
        /// DateFormatOptions.DateFormats - governs every date-only display
        /// (e.g. "Joined") site-wide.
        /// </summary>
        [Required(ErrorMessage = "Date format is required")]
        [Display(Name = "Date Format")]
        public string DateFormat { get; set; } = "dd/MM/yyyy";

        /// <summary>
        /// A .NET custom time format string, restricted to
        /// DateFormatOptions.TimeFormats - combined with DateFormat for
        /// every date+time display (e.g. post/thread timestamps) site-wide.
        /// </summary>
        [Required(ErrorMessage = "Time format is required")]
        [Display(Name = "Time Format")]
        public string TimeFormat { get; set; } = "HH:mm";

        /// <summary>
        /// The forum's display time zone: an IANA id (e.g. "Europe/London")
        /// from the admin dropdown, or blank for the web server's own local
        /// time zone (the behaviour before this setting existed, and the
        /// default). Timestamps are always stored in UTC and converted to this
        /// zone only for display; see MvcBB.Shared.Services.ForumTimeZone.
        /// </summary>
        [StringLength(100, ErrorMessage = "Time zone id cannot exceed 100 characters")]
        // Nullable, with blank kept as blank, so the admin form doesn't treat
        // it as required: "Server local time" posts an empty value.
        [Display(Name = "Time Zone")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string? TimeZoneId { get; set; } = string.Empty;

        /// <summary>
        /// The most emails the host lets the site send in any rolling hour
        /// (e.g. 250). 0 = use AdminEmail:MaxPerHour from the API's
        /// appsettings.json (itself 0 = no limit). Admin emails wait to stay
        /// under it; every email the site sends counts towards it.
        /// </summary>
        [Range(0, 1000000, ErrorMessage = "The hourly sending limit must be 0 or more")]
        [Display(Name = "Host sending limit per hour")]
        public int MaxEmailsPerHour { get; set; }

        /// <summary>The same as MaxEmailsPerHour for any rolling 24 hours. 0 = use AdminEmail:MaxPerDay.</summary>
        [Range(0, 10000000, ErrorMessage = "The daily sending limit must be 0 or more")]
        [Display(Name = "Host sending limit per day")]
        public int MaxEmailsPerDay { get; set; }

        /// <summary>
        /// Whether the home page shows the "Who's online" card (counts of
        /// members, guests and bots seen recently). Added in v1.4.1.
        /// </summary>
        [Display(Name = "Show who's online on the home page")]
        public bool ShowWhosOnline { get; set; } = true;

        /// <summary>
        /// How recently a visitor must have loaded a page to count as online,
        /// in minutes (1 to 60). Added in v1.4.1.
        /// </summary>
        [Range(1, 60, ErrorMessage = "The online window must be between 1 and 60 minutes")]
        [Display(Name = "Online window (minutes)")]
        public int OnlineWindowMinutes { get; set; } = 15;
    }
}
