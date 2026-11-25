namespace MvcBB.Shared.Models.User
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }
        public UserRole Role { get; set; }
        public string RoleName => Role.ToString();
        public string? Signature { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public bool ShowEmail { get; set; }

        /// <summary>Opening a discussion scrolls straight to the first unread post.</summary>
        public bool AutoJumpToUnread { get; set; } = true;

        // Effective display title - CustomTitle if an admin set one, else the
        // computed rank (RankCalculator.GetRank). RankImagePath is only ever
        // populated for a computed rank; a custom title has no badge image.
        public string? RankTitle { get; set; }
        public string? RankImagePath { get; set; }

        /// <summary>The raw admin override, or null if this user has none. Shown in the admin UI so it's clear whether RankTitle is computed or overridden.</summary>
        public string? CustomTitle { get; set; }

        /// <summary>True if the user must set a new password before continuing to use the site.</summary>
        public bool MustChangePassword { get; set; }

        /// <summary>Admin-set lock expiry, or null if not locked. Only populated in the admin user list.</summary>
        public DateTime? LockedUntil { get; set; }

        /// <summary>True while LockedUntil is set and still in the future.</summary>
        public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

        /// <summary>Most recent login IP address. Only populated in the admin user list.</summary>
        public string? LastLoginIp { get; set; }

        /// <summary>
        /// Whether they get emails admins send from the admin panel (false =
        /// opted out). Only populated on the member's own profile and the admin
        /// per-user page; null everywhere else.
        /// </summary>
        public bool? AllowAdminEmails { get; set; }
    }
}
