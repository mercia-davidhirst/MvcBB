namespace MvcBB.Shared.Models.User
{
    public class UserAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserResponse User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
