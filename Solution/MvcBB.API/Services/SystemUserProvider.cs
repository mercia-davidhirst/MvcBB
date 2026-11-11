using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Thread-watch notifications sent as a private message need a sender -
    /// using the actual replying user would misrepresent a system-generated
    /// notification as something they personally wrote. This lazily
    /// creates (once) a reserved, unloginable "System" account to send
    /// from instead, the same way a bot/notifications account works on most
    /// forums. Not seeded up front (unlike the admin account) so this stays
    /// backend-agnostic - SQL Server/PostgreSQL have no user seed data at all
    /// today.
    /// </summary>
    public static class SystemUserProvider
    {
        public const string Username = "System";

        public static string GetOrCreateSystemUserId(IUserRepository userRepository)
        {
            var existing = userRepository.GetByUsername(Username);
            if (existing != null)
            {
                return existing.Id.ToString();
            }

            var created = userRepository.Add(new User
            {
                Username = Username,
                Email = "system@mvcbb.local",
                // Random, never communicated anywhere - this account can't log in.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"), BCrypt.Net.BCrypt.GenerateSalt(12)),
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.User,
                Bio = "Sends automated notifications (e.g. thread-watch replies)."
            });

            return created.Id.ToString();
        }
    }
}
