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

        [Url]
        [StringLength(200)]
        public string? AvatarUrl { get; set; }

        public bool ShowEmail { get; set; }
    }
} 