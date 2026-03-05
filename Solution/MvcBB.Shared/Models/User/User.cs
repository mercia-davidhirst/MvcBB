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
    }
} 