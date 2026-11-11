using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Settings
{
    /// <summary>
    /// Site-wide password complexity requirements, enforced wherever a password
    /// is set (register, admin-create-user, self change-password, reset-password)
    /// - see MvcBB.Shared.Services.PasswordPolicyValidator.
    /// </summary>
    public class PasswordPolicy
    {
        [Range(4, 100, ErrorMessage = "Minimum length must be between 4 and 100")]
        [Display(Name = "Minimum Length")]
        public int MinimumLength { get; set; } = 8;

        [Display(Name = "Require an uppercase letter")]
        public bool RequireUppercase { get; set; }

        [Display(Name = "Require a lowercase letter")]
        public bool RequireLowercase { get; set; }

        [Display(Name = "Require a digit")]
        public bool RequireDigit { get; set; }

        [Display(Name = "Require a special character")]
        public bool RequireSpecialCharacter { get; set; }
    }
}
