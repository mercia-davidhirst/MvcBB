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

        /// <summary>
        /// Only needed when Email changes - the API refuses an email change
        /// without the account's current password, so a hijacked session
        /// can't quietly redirect password resets to someone else's inbox.
        /// </summary>
        [StringLength(200)]
        public string? CurrentPassword { get; set; }
    }
} 