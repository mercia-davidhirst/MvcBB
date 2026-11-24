namespace MvcBB.Shared.Models.AdminEmail
{
    /// <summary>
    /// How many emails of one kind were sent in one time slot - a row from
    /// IEmailSendLogRepository.GetCounts.
    /// </summary>
    public class EmailSendCount
    {
        /// <summary>UTC start of the slot.</summary>
        public DateTime BucketStart { get; set; }
        public EmailKind Kind { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// The emails the API has sent over a period, for the Sending activity
    /// chart: one bucket per hour (24 hours) or per day (7/30 days), plus the
    /// current usage against the host's sending limits.
    /// </summary>
    public class EmailActivity
    {
        /// <summary>"24h", "7d" or "30d".</summary>
        public string Range { get; set; } = "24h";

        /// <summary>True for hourly buckets (compared with the hourly limit), false for daily ones (the daily limit).</summary>
        public bool Hourly { get; set; }

        public List<EmailActivityBucket> Buckets { get; set; } = new();

        /// <summary>
        /// 24-hour view only: emails sent in the 60 minutes up to each point,
        /// every 10 minutes - what a host's "per hour" limit is usually
        /// measured against, rather than clock hours.
        /// </summary>
        public List<EmailRollingPoint> RollingHour { get; set; } = new();

        public EmailSendUsage Usage { get; set; } = new();
    }

    /// <summary>One bar on the chart: the emails sent between StartUtc and EndUtc, by kind.</summary>
    public class EmailActivityBucket
    {
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public int AdminEmail { get; set; }
        public int PasswordReset { get; set; }
        public int Notification { get; set; }
        public int Registration { get; set; }
        public int Other { get; set; }

        public int Total => AdminEmail + PasswordReset + Notification + Registration + Other;

        public int CountOf(EmailKind kind) => kind switch
        {
            EmailKind.AdminEmail => AdminEmail,
            EmailKind.PasswordReset => PasswordReset,
            EmailKind.Notification => Notification,
            EmailKind.Registration => Registration,
            _ => Other
        };

        public void Add(EmailKind kind, int count)
        {
            switch (kind)
            {
                case EmailKind.AdminEmail: AdminEmail += count; break;
                case EmailKind.PasswordReset: PasswordReset += count; break;
                case EmailKind.Notification: Notification += count; break;
                case EmailKind.Registration: Registration += count; break;
                default: Other += count; break;
            }
        }
    }

    /// <summary>A point on the rolling 60-minute line.</summary>
    public class EmailRollingPoint
    {
        public DateTime AtUtc { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Emails sent recently against the host's sending limits (Admin &gt;
    /// Settings, or AdminEmail:MaxPerHour / MaxPerDay), and what that leaves
    /// for admin emails right now.
    /// </summary>
    public class EmailSendUsage
    {
        public int SentLastHour { get; set; }
        public int SentLast24Hours { get; set; }

        /// <summary>0 = no hourly limit.</summary>
        public int LimitPerHour { get; set; }

        /// <summary>0 = no daily limit.</summary>
        public int LimitPerDay { get; set; }

        /// <summary>How much of each limit is kept back for the site's own emails (password resets etc.).</summary>
        public int ReservePerHour { get; set; }
        public int ReservePerDay { get; set; }

        /// <summary>How many admin emails could go out now, or null when no limit is set.</summary>
        public int? AdminAllowanceNow { get; set; }

        /// <summary>When AdminAllowanceNow is 0: when admin emails can carry on (UTC).</summary>
        public DateTime? NextFreeAtUtc { get; set; }

        public bool HasLimits => LimitPerHour > 0 || LimitPerDay > 0;
    }
}
