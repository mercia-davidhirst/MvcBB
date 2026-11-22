using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Report
{
    public class Report
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;

        public string ReporterUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ReportType Type { get; set; }
        public int ContentId { get; set; }
        
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public string ModeratorNotes { get; set; } = string.Empty;
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// For <see cref="ReportType.SiteIssue"/> reports: the page the reporter
        /// was on when they raised it (a site-relative path, or null if they
        /// chose not to include it). Always null for other report types.
        /// </summary>
        public string? PageUrl { get; set; }
    }
} 