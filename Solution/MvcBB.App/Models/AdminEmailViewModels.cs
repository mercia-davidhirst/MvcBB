using System.ComponentModel.DataAnnotations;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.User;

namespace MvcBB.App.Models
{
    /// <summary>The Email Users compose form, and the confirmation step that repeats it in hidden fields.</summary>
    public class AdminEmailComposeViewModel
    {
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(20000)]
        [Display(Name = "Message")]
        public string Body { get; set; } = string.Empty;

        public AdminEmailAudience Audience { get; set; } = AdminEmailAudience.AllUsers;

        public UserRole? Role { get; set; }

        /// <summary>For Audience = SingleUser: typed on the form, or filled in from a user's admin page.</summary>
        [StringLength(50)]
        public string? Username { get; set; }

        public AdminEmailSenderMode SenderMode { get; set; } = AdminEmailSenderMode.Admin;

        [Display(Name = "Important service alert")]
        public bool IsImportant { get; set; }

        /// <summary>The signed-in admin's address, for the "from me" option's label.</summary>
        public string? AdminEmailAddress { get; set; }

        /// <summary>Set on the confirmation step.</summary>
        public AdminEmailPreview? Preview { get; set; }

        /// <summary>The send log under the form.</summary>
        public IEnumerable<AdminEmail> RecentEmails { get; set; } = Array.Empty<AdminEmail>();

        public SendAdminEmailRequest ToRequest(int? userId) => new()
        {
            Subject = Subject,
            Body = Body,
            Audience = Audience,
            Role = Audience == AdminEmailAudience.Role ? Role : null,
            UserId = Audience == AdminEmailAudience.SingleUser ? userId : null,
            SenderMode = SenderMode,
            IsImportant = IsImportant
        };
    }

    public static class AdminEmailDisplay
    {
        public static string Audience(AdminEmail email) => email.Audience switch
        {
            AdminEmailAudience.AllUsers => "All members",
            AdminEmailAudience.Role => email.Role switch
            {
                UserRole.User => "Role: User",
                UserRole.Moderator => "Role: Moderator",
                UserRole.Administrator => "Role: Administrator",
                _ => "Role"
            },
            AdminEmailAudience.SingleUser => "One member",
            _ => email.Audience.ToString()
        };

        public static string Status(AdminEmailStatus status) => status switch
        {
            AdminEmailStatus.Queued => "Queued",
            AdminEmailStatus.Sending => "Sending",
            AdminEmailStatus.Completed => "Sent",
            AdminEmailStatus.CompletedWithErrors => "Sent with errors",
            AdminEmailStatus.Interrupted => "Interrupted",
            _ => status.ToString()
        };

        /// <summary>Badge classes that read in both the light and dark schemes.</summary>
        public static string StatusBadge(AdminEmailStatus status) => status switch
        {
            AdminEmailStatus.Queued => "bg-secondary",
            AdminEmailStatus.Sending => "bg-info text-dark",
            AdminEmailStatus.Completed => "bg-success",
            AdminEmailStatus.CompletedWithErrors => "bg-warning text-dark",
            AdminEmailStatus.Interrupted => "bg-danger",
            _ => "bg-secondary"
        };

        public static string From(AdminEmail email) =>
            string.IsNullOrWhiteSpace(email.FromName) ? email.FromAddress : $"{email.FromName} <{email.FromAddress}>";

        public static bool IsFinished(AdminEmailStatus status) =>
            status is not (AdminEmailStatus.Queued or AdminEmailStatus.Sending);
    }
}
