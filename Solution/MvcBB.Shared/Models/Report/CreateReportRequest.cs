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
    }
} 