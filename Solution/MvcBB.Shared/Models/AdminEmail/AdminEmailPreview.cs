namespace MvcBB.Shared.Models.AdminEmail
{
    /// <summary>
    /// What sending a SendAdminEmailRequest would do, without sending it - shown
    /// on the confirmation step so the admin sees the numbers and the sender
    /// before anything goes out.
    /// </summary>
    public class AdminEmailPreview
    {
        /// <summary>How many members it would go to.</summary>
        public int RecipientCount { get; set; }

        /// <summary>Members in the chosen audience left out because they opted out (0 for an important alert).</summary>
        public int OptedOutCount { get; set; }

        /// <summary>The From header as it would be sent, e.g. "admin via MvcBB &lt;noreply@example.com&gt;".</summary>
        public string From { get; set; } = string.Empty;

        /// <summary>The Reply-To address, or null for none.</summary>
        public string? ReplyTo { get; set; }

        /// <summary>True when SenderMode = Admin but AdminEmail:AllowAdminFromAddress is off, so the admin's address went to Reply-To instead.</summary>
        public bool AdminAddressMovedToReplyTo { get; set; }

        /// <summary>For Audience = SingleUser: the recipient's username.</summary>
        public string? SingleUserName { get; set; }

        /// <summary>How the body ends for recipients (the opt-out note) - shown under the message on the confirmation step.</summary>
        public string Footer { get; set; } = string.Empty;
    }
}
