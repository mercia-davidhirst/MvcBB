using System.ComponentModel.DataAnnotations;
using MvcBB.Shared.Models.User;

namespace MvcBB.Shared.Models.AdminEmail
{
    public class SendAdminEmailRequest
    {
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>Plain text. "{username}" is replaced with each recipient's username.</summary>
        [Required]
        [StringLength(20000)]
        public string Body { get; set; } = string.Empty;

        public AdminEmailAudience Audience { get; set; } = AdminEmailAudience.AllUsers;

        /// <summary>Required when Audience = Role.</summary>
        public UserRole? Role { get; set; }

        /// <summary>Required when Audience = SingleUser.</summary>
        public int? UserId { get; set; }

        public AdminEmailSenderMode SenderMode { get; set; } = AdminEmailSenderMode.Admin;

        /// <summary>Important service alert (e.g. planned maintenance, a security notice): also goes to members who opted out of admin emails.</summary>
        public bool IsImportant { get; set; }
    }
}
