namespace MvcBB.Shared.Models.AdminEmail
{
    /// <summary>Just what's needed to address one admin email - see IAdminEmailRepository.GetRecipients.</summary>
    public class AdminEmailRecipient
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
