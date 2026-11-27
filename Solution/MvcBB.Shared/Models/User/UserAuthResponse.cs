namespace MvcBB.Shared.Models.User
{
    public class UserAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserResponse User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Only when the member ticked "Remember me": a long-lived, single-use
        /// token that swaps for a fresh access token (and a new refresh token)
        /// at POST api/users/refresh. Null otherwise.
        /// </summary>
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }
}
