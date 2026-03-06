using Microsoft.Extensions.DependencyInjection;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.InMemory
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers in-memory implementations of all forum repositories and seeds the store with an admin user if empty.
        /// </summary>
        public static IServiceCollection AddInMemoryRepositories(this IServiceCollection services)
        {
            var store = new InMemoryStore();
            SeedAdminUserIfEmpty(store);
            services.AddSingleton(store);

            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<IBoardRepository, InMemoryBoardRepository>();
            services.AddSingleton<IForumThreadRepository, InMemoryForumThreadRepository>();
            services.AddSingleton<IPostRepository, InMemoryPostRepository>();
            services.AddSingleton<IMessageRepository, InMemoryMessageRepository>();
            services.AddSingleton<IReportRepository, InMemoryReportRepository>();

            return services;
        }

        private static void SeedAdminUserIfEmpty(InMemoryStore store)
        {
            if (store.Users.Count > 0)
                return;

            store.Users.Add(new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.Administrator
            });
        }
    }
}
