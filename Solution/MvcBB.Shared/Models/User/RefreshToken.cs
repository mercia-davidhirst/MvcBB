namespace MvcBB.Shared.Models.User
{
    /// <summary>
    /// A "Remember me" refresh token. Only TokenHash (a SHA-256 hash) is ever
    /// stored; the raw token lives only in the member's App cookie.
    ///
    /// Each token is single-use: swapping it for a new access token marks it
    /// used and issues a replacement in the same family. Presenting a token
    /// that has already been used means it was copied, so the whole family
    /// (that device's sign-in) is revoked.
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>One sign-in on one device; shared by every token rotated from it.</summary>
        public Guid FamilyId { get; set; }

        /// <summary>
        /// JwtTokenService.ComputeSessionStamp of the account when the token was
        /// issued, so a password change, role change or lock ends it.
        /// </summary>
        public string SessionStamp { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? CreatedIp { get; set; }
    }

    /// <summary>Body of POST api/users/refresh and POST api/users/logout.</summary>
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>Set by MvcBB.App from the request that reached it.</summary>
        public string? IpAddress { get; set; }
    }
}
