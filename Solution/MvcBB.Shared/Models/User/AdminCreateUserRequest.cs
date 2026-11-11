using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.User
{
    public class AdminCreateUserRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(UserRole))]
        public UserRole Role { get; set; } = UserRole.User;

        [Display(Name = "Require password change on first login")]
        public bool MustChangePassword { get; set; } = true;
    }
}
