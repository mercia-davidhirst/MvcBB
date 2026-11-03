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
    }
}
