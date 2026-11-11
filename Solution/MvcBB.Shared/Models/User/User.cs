using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.User
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }
        public string? Signature { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public bool ShowEmail { get; set; }

        /// <summary>
        /// Admin-set override shown next to the user's name instead of their
        /// computed rank (see RankCalculator.GetRank) - null means "use the
        /// computed rank" (the default for everyone). Not self-service; only
        /// an admin can set this (UsersController.SetCustomTitle).
        /// </summary>
        public string? CustomTitle { get; set; }

        /// <summary>
        /// Admin-set flag forcing the user to set a new password before they can
        /// use the site again. Cleared automatically the next time their
        /// password is changed (see UsersController.ChangePassword).
        /// </summary>
        public bool MustChangePassword { get; set; }

        /// <summary>
        /// Admin-set lock expiry - null means not locked. A value in the future
        /// blocks login (see UsersController.Login); once it's in the past the
        /// account is effectively unlocked again without any admin action.
        /// </summary>
        public DateTime? LockedUntil { get; set; }

        /// <summary>Most recent login IP address, set by UsersController.Login.</summary>
        public string? LastLoginIp { get; set; }
    }
} 