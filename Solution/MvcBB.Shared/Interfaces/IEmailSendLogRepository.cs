using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.Shared.Interfaces
{
    /// <summary>
    /// When the API sent each email and what kind it was - no addresses or
    /// subjects. Feeds the Sending activity chart and refills the send-limit
    /// count after a restart. Old rows are pruned (AdminEmail:SendLogRetentionDays).
    /// </summary>
    public interface IEmailSendLogRepository
    {
        void Add(DateTime sentAtUtc, EmailKind kind);

        /// <summary>
        /// Emails sent from fromUtc (inclusive) to toUtc (exclusive), counted
        /// per kind in slots of bucketMinutes starting at fromUtc. Slots with
        /// nothing sent are left out. Ordered by slot, then kind.
        /// </summary>
        IReadOnlyList<EmailSendCount> GetCounts(DateTime fromUtc, DateTime toUtc, int bucketMinutes);

        /// <summary>When each email after sinceUtc was sent, oldest first.</summary>
        IReadOnlyList<DateTime> GetSentTimesSince(DateTime sinceUtc);

        /// <summary>Deletes everything sent before beforeUtc. Returns how many rows went.</summary>
        int Prune(DateTime beforeUtc);
    }
}
