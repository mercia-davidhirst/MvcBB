using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.API.Services
{
    /// <summary>
    /// The rules for what an admin email looks like - who it's from, and the
    /// body each member gets - kept free of I/O so they're easy to test.
    /// </summary>
    public static class AdminEmailComposer
    {
        public const string UsernamePlaceholder = "{username}";

        public record Sender(string FromAddress, string? FromName, string? ReplyToAddress, string? ReplyToName, bool AdminAddressMovedToReplyTo);

        /// <summary>
        /// Works out the From and Reply-To for an admin email.
        /// NoReply: the site's no-reply address, named after the site.
        /// Admin with allowAdminFromAddress: the admin's own address and name.
        /// Admin without it: the site's no-reply address shown as
        /// "admin via Site", with the admin's address as Reply-To - a relay can
        /// only send From domains it's authorised for (SPF/DKIM/DMARC), so this
        /// is what actually gets delivered on a typical setup.
        /// </summary>
        public static Sender ResolveSender(
            AdminEmailSenderMode mode,
            string adminUsername,
            string adminEmail,
            string siteName,
            string noReplyAddress,
            bool allowAdminFromAddress)
        {
            if (mode == AdminEmailSenderMode.NoReply)
            {
                return new Sender(noReplyAddress, siteName, null, null, false);
            }

            if (allowAdminFromAddress)
            {
                return new Sender(adminEmail, adminUsername, null, null, false);
            }

            return new Sender(noReplyAddress, $"{adminUsername} via {siteName}", adminEmail, adminUsername, true);
        }

        /// <summary>The From header as a person would read it, e.g. "admin via MvcBB &lt;noreply@example.com&gt;".</summary>
        public static string FormatAddress(string address, string? name) =>
            string.IsNullOrWhiteSpace(name) ? address : $"{name} <{address}>";

        /// <summary>
        /// The line added under every admin email: how to opt out, or for an
        /// important alert, why it arrived anyway.
        /// </summary>
        public static string BuildFooter(string siteName, string profileUrl, bool isImportant) =>
            isImportant
                ? $"This is an important service notice from {siteName}. It goes to every member, including those who have turned off emails from the {siteName} team."
                : $"You're receiving this because you're a member of {siteName}. To stop emails from the {siteName} team, untick \"{AdminEmailText.OptOutLabel}\" on your profile: {profileUrl}";

        /// <summary>The body one member receives: {username} filled in, then the footer.</summary>
        public static string BuildBody(string body, string recipientUsername, string siteName, string profileUrl, bool isImportant)
        {
            var personalised = body.Replace(UsernamePlaceholder, recipientUsername, StringComparison.OrdinalIgnoreCase).TrimEnd();
            return $"{personalised}\n\n--\n{BuildFooter(siteName, profileUrl, isImportant)}";
        }

        /// <summary>Longest failure reason kept (AdminEmailFailures.Reason / admin_email_failures.reason).</summary>
        public const int MaxFailureReasonLength = 1000;

        /// <summary>
        /// Why sending to one member failed, as shown on the details page: the
        /// innermost exception's message (wrappers like "One or more errors
        /// occurred" say little), trimmed and cut to MaxFailureReasonLength.
        /// </summary>
        public static string FailureReason(Exception error)
        {
            var reason = string.Empty;
            for (var current = error; current != null; current = current.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                {
                    reason = current.Message;
                }
            }

            reason = reason.Trim();
            if (reason.Length == 0)
            {
                reason = error.GetType().Name;
            }

            return reason.Length > MaxFailureReasonLength ? reason[..MaxFailureReasonLength] : reason;
        }

        /// <summary>The ready-to-send message for one member.</summary>
        public static EmailMessage BuildMessage(AdminEmail email, AdminEmailRecipient recipient, string siteName, string profileUrl) => new()
        {
            To = recipient.Email,
            Subject = email.Subject,
            Body = BuildBody(email.Body, recipient.Username, siteName, profileUrl, email.IsImportant),
            FromAddress = email.FromAddress,
            FromName = email.FromName,
            ReplyToAddress = email.ReplyToAddress,
            ReplyToName = string.IsNullOrEmpty(email.ReplyToAddress) ? null : email.SentByUsername,
            Kind = EmailKind.AdminEmail
        };

        /// <summary>Splits recipients into batches of at most batchSize (minimum 1).</summary>
        public static IEnumerable<IReadOnlyList<T>> Batch<T>(IReadOnlyList<T> items, int batchSize)
        {
            var size = Math.Max(1, batchSize);
            for (var i = 0; i < items.Count; i += size)
            {
                yield return items.Skip(i).Take(size).ToList();
            }
        }
    }
}
