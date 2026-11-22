namespace MvcBB.Shared.Models.Report
{
    public enum ReportType
    {
        Thread,
        Post,
        Message,
        User,

        /// <summary>
        /// A problem with the site itself (a broken page, an error, a layout
        /// glitch), raised from the navbar's "Report an issue" button. Not
        /// about any post, thread, message or member, so ContentId is always
        /// 0; the page the reporter was on, if they chose to include it, is in
        /// Report.PageUrl instead. Added in v1.1.0.
        /// </summary>
        SiteIssue
    }

    public enum ReportStatus
    {
        Pending,
        Investigating,
        Resolved,
        Dismissed
    }
} 