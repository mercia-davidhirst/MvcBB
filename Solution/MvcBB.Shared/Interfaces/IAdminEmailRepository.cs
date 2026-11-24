using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.User;

namespace MvcBB.Shared.Interfaces
{
    /// <summary>
    /// Admin emails to members: the send log, who's opted out of them, and
    /// who a given audience resolves to. The opt-out lives in its own table
    /// (not a Users column) so that the many existing user queries and the
    /// whole-row user update can't silently reset it.
    /// </summary>
    public interface IAdminEmailRepository
    {
        AdminEmail Add(AdminEmail email);

        /// <summary>Saves Status, RecipientCount, SentCount, FailedCount and CompletedAt.</summary>
        void UpdateProgress(AdminEmail email);

        AdminEmail? GetById(int id);

        /// <summary>Newest first.</summary>
        IReadOnlyList<AdminEmail> GetRecent(int take);

        /// <summary>Marks every Queued/Sending email as Interrupted - run at API startup, since nothing can still be sending them. Returns how many were changed.</summary>
        int MarkUnfinishedInterrupted();

        bool IsOptedOut(int userId);
        void SetOptedOut(int userId, bool optedOut);

        /// <summary>
        /// Who an audience resolves to, ordered by user id. Deleted (anonymized)
        /// accounts and accounts without an email address are never included.
        /// role narrows to one role; userId narrows to one account (both null =
        /// everyone). Opted-out members are left out unless includeOptedOut.
        /// </summary>
        IReadOnlyList<AdminEmailRecipient> GetRecipients(UserRole? role, int? userId, bool includeOptedOut);

        /// <summary>Same filter as GetRecipients, as counts: everyone in the audience, and how many of those opted out.</summary>
        (int Total, int OptedOut) CountRecipients(UserRole? role, int? userId);
    }
}
