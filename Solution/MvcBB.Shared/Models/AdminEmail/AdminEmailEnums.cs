namespace MvcBB.Shared.Models.AdminEmail
{
    /// <summary>Who an admin email goes to.</summary>
    public enum AdminEmailAudience
    {
        /// <summary>Every account except Deleted (anonymized) ones.</summary>
        AllUsers = 0,

        /// <summary>Every account with one role (AdminEmail.Role).</summary>
        Role = 1,

        /// <summary>One account (AdminEmail.UserId).</summary>
        SingleUser = 2
    }

    /// <summary>Which address an admin email is sent from.</summary>
    public enum AdminEmailSenderMode
    {
        /// <summary>
        /// The sending admin's own address. Only used as the actual From address
        /// when AdminEmail:AllowAdminFromAddress is true; otherwise the site's
        /// address is the From (shown with the admin's name) and the admin's
        /// address goes in Reply-To. See AdminEmailComposer.ResolveSender.
        /// </summary>
        Admin = 0,

        /// <summary>The site's no-reply address (Smtp:FromEmail), with no Reply-To.</summary>
        NoReply = 1
    }

    public enum AdminEmailStatus
    {
        /// <summary>Waiting for the background sender to pick it up.</summary>
        Queued = 0,

        /// <summary>Being sent, a chunk at a time.</summary>
        Sending = 1,

        /// <summary>Every recipient was sent to.</summary>
        Completed = 2,

        /// <summary>Finished, but sending failed for at least one recipient (see the API log).</summary>
        CompletedWithErrors = 3,

        /// <summary>
        /// The API stopped (restart, recycle, crash) before this finished. It's
        /// deliberately not resumed on startup: there's no per-recipient record,
        /// so resuming could email the same people twice.
        /// </summary>
        Interrupted = 4
    }
}
