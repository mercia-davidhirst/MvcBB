using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.User
{
    public class UpdateUserRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Signature { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [StringLength(200)]
        [RegularExpression(MvcBB.Shared.Services.ImagePaths.AvatarPattern, ErrorMessage = "Invalid avatar path.")]
        public string? AvatarUrl { get; set; }

        public bool ShowEmail { get; set; }

        /// <summary>Scroll straight to the first unread post when opening a discussion. Null leaves the current choice unchanged.</summary>
        public bool? AutoJumpToUnread { get; set; }

        /// <summary>Receive emails admins send from the admin panel. Null leaves the current choice unchanged.</summary>
        public bool? AllowAdminEmails { get; set; }

        /// <summary>
        /// True when this request sets the birthday (BirthDay, BirthMonth,
        /// BirthYear, all null to remove it). False (an older client that
        /// doesn't send it) leaves the birthday as it is.
        /// </summary>
        public bool UpdateBirthday { get; set; }

        [Range(1, 31)]
        public int? BirthDay { get; set; }

        [Range(1, 12)]
        public int? BirthMonth { get; set; }

        [Range(1900, 9999)]
        public int? BirthYear { get; set; }

        /// <summary>
        /// Only needed when Email changes - the API refuses an email change
        /// without the account's current password, so a hijacked session
        /// can't quietly redirect password resets to someone else's inbox.
        /// </summary>
        [StringLength(200)]
        public string? CurrentPassword { get; set; }
    }
} 