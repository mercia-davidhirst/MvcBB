using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.API.Services
{
    /// <summary>
    /// One outgoing email with optional sender overrides - see
    /// IEmailSender.SendAsync(EmailMessage). Null FromAddress/FromName fall
    /// back to Smtp:FromEmail/Smtp:FromName.
    /// </summary>
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;

        /// <summary>Plain text.</summary>
        public string Body { get; set; } = string.Empty;

        public string? FromAddress { get; set; }
        public string? FromName { get; set; }
        public string? ReplyToAddress { get; set; }
        public string? ReplyToName { get; set; }

        /// <summary>What it's for, as recorded in the send log. Only admin emails ever wait for the host's sending limits.</summary>
        public EmailKind Kind { get; set; } = EmailKind.Other;
    }
}
