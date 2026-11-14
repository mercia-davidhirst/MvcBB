using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Settings
{
    public class DisplaySettings
    {
        [Range(1, 100, ErrorMessage = "Threads per page must be between 1 and 100")]
        public int ThreadsPerPage { get; set; } = 20;

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
    }
}
