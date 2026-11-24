using MvcBB.Shared.Models.User;

namespace MvcBB.Shared.Models.AdminEmail
{
    /// <summary>
    /// One email sent by an admin from the admin panel, kept as a send log:
    /// what was sent, by whom, to which audience and how far sending got.
    /// Backed by dbo.AdminEmails / public.admin_emails.
    /// </summary>
    public class AdminEmail
    {
        public int Id { get; set; }
        public int SentByUserId { get; set; }

        /// <summary>The sending admin's username at the time, so the log still reads correctly if they're renamed or deleted.</summary>
        public string SentByUsername { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public AdminEmailAudience Audience { get; set; }

        /// <summary>Only for Audience = Role.</summary>
        public UserRole? Role { get; set; }

        /// <summary>Only for Audience = SingleUser.</summary>
        public int? UserId { get; set; }

        public AdminEmailSenderMode SenderMode { get; set; }

        /// <summary>The From address actually used (after AdminEmail:AllowAdminFromAddress was applied).</summary>
        public string FromAddress { get; set; } = string.Empty;

        /// <summary>The From display name actually used, or null for the bare address.</summary>
        public string? FromName { get; set; }

        /// <summary>The Reply-To address actually used, or null for none.</summary>
        public string? ReplyToAddress { get; set; }

        /// <summary>Important service alert: also sent to members who opted out of admin emails.</summary>
        public bool IsImportant { get; set; }

        public AdminEmailStatus Status { get; set; }
        public int RecipientCount { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
    }
}
