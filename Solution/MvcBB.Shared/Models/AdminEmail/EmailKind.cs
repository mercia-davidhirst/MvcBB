namespace MvcBB.Shared.Models.AdminEmail
{
    /// <summary>
    /// What an email the API sent was for, as recorded in the send log
    /// (dbo.EmailSendLog.Kind / public.email_send_log.kind) and shown on the
    /// Sending activity chart. Every kind counts towards the host's sending
    /// limits; only AdminEmail ever waits for them.
    /// </summary>
    public enum EmailKind : byte
    {
        /// <summary>Anything not marked otherwise (e.g. an email a mod sends).</summary>
        Other = 0,

        /// <summary>Emails admins send from Email Users, test sends included.</summary>
        AdminEmail = 1,

        PasswordReset = 2,

        /// <summary>Thread reply notifications.</summary>
        Notification = 3,

        /// <summary>Registration emails (none are sent by the core yet; kept for mods and later use).</summary>
        Registration = 4
    }
}
