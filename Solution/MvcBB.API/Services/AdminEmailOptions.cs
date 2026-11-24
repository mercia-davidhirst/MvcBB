namespace MvcBB.API.Services
{
    /// <summary>
    /// The AdminEmail:* keys in appsettings.json, for emails admins send to
    /// members from the admin panel.
    /// </summary>
    public class AdminEmailOptions
    {
        public const string SectionName = "AdminEmail";

        /// <summary>
        /// When true, "send from my address" puts the admin's own address in
        /// From. Only turn this on if the SMTP relay is allowed to send for
        /// the admins' email domains (SPF includes the relay and it DKIM-signs
        /// for them) - otherwise receiving servers fail DMARC and reject the
        /// mail or file it as spam, which is almost certain for Gmail/Outlook
        /// addresses. When false (the default) the site's address (Smtp:FromEmail)
        /// is the From, shown as "admin via Site name", and the admin's address
        /// goes in Reply-To so answers still reach them.
        /// </summary>
        public bool AllowAdminFromAddress { get; set; }

        /// <summary>How many emails go out back to back before pausing. At least 1.</summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>The pause between batches, so a large send doesn't trip the relay's rate limits. 0 = no pause.</summary>
        public int BatchDelaySeconds { get; set; } = 30;

        /// <summary>
        /// The most emails the API may send in any rolling hour, for hosts that
        /// cap it (e.g. 250 an hour). Counts every email the API sends, not just
        /// admin emails. Admin emails wait when they'd go over it; other emails
        /// (password resets etc.) are never held up. 0 = no limit. Only used
        /// when Admin &gt; Settings leaves its own hourly limit at 0.
        /// </summary>
        public int MaxPerHour { get; set; }

        /// <summary>The same as MaxPerHour for any rolling 24 hours. 0 = no limit.</summary>
        public int MaxPerDay { get; set; }

        /// <summary>How many days the email send log (the Sending activity chart) keeps. At least 1.</summary>
        public int SendLogRetentionDays { get; set; } = 30;
    }
}
