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
    }
}
