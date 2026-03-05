using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Report
{
    public class UpdateReportRequest
    {
        [Required(ErrorMessage = "Status is required")]
        public ReportStatus Status { get; set; }

        [StringLength(1000, ErrorMessage = "Moderator notes cannot exceed 1000 characters")]
        public string ModeratorNotes { get; set; } = string.Empty;
    }
} 