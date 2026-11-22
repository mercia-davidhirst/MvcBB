namespace MvcBB.Shared.Models.Report
{
    /// <summary>
    /// Human-readable labels for <see cref="ReportType"/>, shared by the
    /// report list, report details and the API's report stats.
    /// </summary>
    public static class ReportTypeNames
    {
        public static string GetDisplayName(ReportType type) => type switch
        {
            ReportType.SiteIssue => "Site issue",
            _ => type.ToString()
        };

        /// <summary>Report statuses that still need a moderator's attention.</summary>
        public static bool IsOpen(ReportStatus status) =>
            status == ReportStatus.Pending || status == ReportStatus.Investigating;
    }
}
