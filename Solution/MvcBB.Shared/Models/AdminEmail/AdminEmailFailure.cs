namespace MvcBB.Shared.Models.AdminEmail
{
    /// <summary>
    /// One member an admin email couldn't be sent to, and why - shown on the
    /// email's details page. Backed by dbo.AdminEmailFailures /
    /// public.admin_email_failures.
    /// </summary>
    public class AdminEmailFailure
    {
        public int Id { get; set; }
        public int AdminEmailId { get; set; }
        public int UserId { get; set; }

        /// <summary>The member's username at the time, so the list still reads correctly if they're renamed or deleted.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>The address the email was sent to.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Why sending failed (the mail server's or mail library's message), at most 1000 characters.</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>UTC.</summary>
        public DateTime FailedAt { get; set; }
    }
}
