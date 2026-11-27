using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.User
{
    public class ChangePasswordRequest
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword))]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Set by MvcBB.App (never taken from the form) when the session making
        /// the change was signed in with "Remember me". Changing the password
        /// revokes every refresh token, so the API issues this session a new
        /// one to carry on with.
        /// </summary>
        public bool KeepSignedIn { get; set; }
    }
} 