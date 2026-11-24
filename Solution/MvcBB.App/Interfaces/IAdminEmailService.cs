using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.App.Interfaces
{
    /// <summary>Admin-only: emails from admins to members, via the API's api/admin-emails endpoints.</summary>
    public interface IAdminEmailService
    {
        Task<AdminEmailPreview> PreviewAsync(SendAdminEmailRequest request);

        /// <summary>Queues the email; it's sent in the background. Returns its send log entry.</summary>
        Task<AdminEmail> SendAsync(SendAdminEmailRequest request);

        /// <summary>Sends it to the signed-in admin only, straight away. Returns the API's confirmation message.</summary>
        Task<string> SendTestAsync(SendAdminEmailRequest request);

        Task<IEnumerable<AdminEmail>> GetRecentAsync(int take = 50);
        Task<AdminEmail?> GetAsync(int id);

        /// <summary>The members the email couldn't be sent to, and why, oldest first (empty if the email isn't found).</summary>
        Task<IEnumerable<AdminEmailFailure>> GetFailuresAsync(int id);

        /// <summary>For an Interrupted email: how many members resuming it would send to.</summary>
        Task<int> GetResumeCountAsync(int id);

        /// <summary>Queues an Interrupted email again, for the members it hasn't reached. Throws ServiceException with the API's reason if it can't be resumed.</summary>
        Task<AdminEmail> ResumeAsync(int id);

        /// <summary>Emails sent recently against the host's sending limits.</summary>
        Task<EmailSendUsage> GetUsageAsync();

        /// <summary>Emails sent over "24h", "7d" or "30d", for the Sending activity chart.</summary>
        Task<EmailActivity> GetActivityAsync(string range);
    }
}
