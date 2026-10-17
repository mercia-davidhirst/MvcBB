namespace MvcBB.Shared.Models.User
{
    /// <summary>
    /// A single-use password reset token. Only TokenHash (a SHA-256 hash) is
    /// ever persisted - the raw token exists only in the emailed link, never
    /// in the database, so a database leak can't be used to reset accounts.
    /// </summary>
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
