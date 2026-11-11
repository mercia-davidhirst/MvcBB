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
        // Effective display title - CustomTitle if an admin set one, else the
        // computed rank (RankCalculator.GetRank). RankImagePath is only ever
        // populated for a computed rank; a custom title has no badge image.
        public string? RankTitle { get; set; }
        public string? RankImagePath { get; set; }

        /// <summary>The raw admin override, or null if this user has none. Shown in the admin UI so it's clear whether RankTitle is computed or overridden.</summary>
        public string? CustomTitle { get; set; }
    }
}
