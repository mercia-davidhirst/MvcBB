using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Report
{
    public class CreateReportRequest
    {
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Report type is required")]
        public ReportType Type { get; set; }

        [Required(ErrorMessage = "Content ID is required")]
        public int ContentId { get; set; }

        /// <summary>
        /// Only used for <see cref="ReportType.SiteIssue"/>: the page the
        /// reporter was on, if they chose to include it. Must be a
        /// site-relative path; anything else is dropped by the API.
        /// </summary>
        [StringLength(ReportPageUrl.MaxLength, ErrorMessage = "Page address is too long")]
        public string? PageUrl { get; set; }
    }
} 