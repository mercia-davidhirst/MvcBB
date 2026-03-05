namespace MvcBB.Shared.Models.Report
{
    public class ReportResponse
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string ReporterUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ReportType Type { get; set; }
        public int ContentId { get; set; }
        public ReportStatus Status { get; set; }
        public string ModeratorNotes { get; set; } = string.Empty;
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedByUserId { get; set; } = string.Empty;

        // Additional properties for UI display
        public string TypeName => Type.ToString();
        public string StatusName => Status.ToString();
        public string ContentLink { get; set; } = string.Empty;
        public string ContentTitle { get; set; } = string.Empty;
        public string ContentAuthorId { get; set; } = string.Empty;
        public DateTime ContentCreatedAt { get; set; }
    }
} 