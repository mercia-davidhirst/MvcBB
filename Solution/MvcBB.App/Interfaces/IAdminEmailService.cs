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
    }
}
