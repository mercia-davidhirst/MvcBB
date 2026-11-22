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

        /// <summary>See <see cref="Report.PageUrl"/>.</summary>
        public string? PageUrl { get; set; }

        /// <summary>False for a site issue, which has no post/thread/message/member behind it.</summary>
        public bool HasContent => Type != ReportType.SiteIssue;

        // Additional properties for UI display
        public string TypeName => ReportTypeNames.GetDisplayName(Type);
        public string StatusName => Status.ToString();
        public string ReporterUsername { get; set; } = string.Empty;
        public string ResolvedByUsername { get; set; } = string.Empty;
        public string ContentLink { get; set; } = string.Empty;

        /// <summary>
        /// The thread the reported content lives in: the thread itself for a
        /// Thread report, the post's thread for a Post report (ContentId is
        /// the post's id there, not a thread id). Null for other types, or when
        /// the content no longer exists.
        /// </summary>
        public int? ContentThreadId { get; set; }

        /// <summary>
        /// For a Message report only, and only on the moderator/administrator
        /// report details response (GET api/reports/{id}): the reported private
        /// message's body (raw BBCode, rendered by the view like any message).
        /// Null everywhere else, including the response a reporter gets back
        /// when creating a report, and when the message no longer exists. The
        /// normal message endpoints stay sender/recipient only.
        /// </summary>
        public string? MessageContent { get; set; }

        /// <summary>With <see cref="MessageContent"/>: the message's recipient (the sender is ContentAuthorUsername).</summary>
        public string? MessageRecipientUserId { get; set; }

        public string MessageRecipientUsername { get; set; } = string.Empty;
        public string ContentTitle { get; set; } = string.Empty;
        public string ContentAuthorId { get; set; } = string.Empty;
        public string ContentAuthorUsername { get; set; } = string.Empty;
        public DateTime ContentCreatedAt { get; set; }
    }
} 