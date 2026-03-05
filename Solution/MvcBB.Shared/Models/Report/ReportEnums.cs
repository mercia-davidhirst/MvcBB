namespace MvcBB.Shared.Models.Report
{
    public enum ReportType
    {
        Thread,
        Post,
        Message
    }

    public enum ReportStatus
    {
        Pending,
        Investigating,
        Resolved,
        Dismissed
    }
} 