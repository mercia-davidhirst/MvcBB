using Microsoft.Extensions.DependencyInjection;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.SQL
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers SQL-backed implementations of all forum repositories.
        /// Add DbContext and connection string configuration when implementing with EF Core.
        /// </summary>
        public static IServiceCollection AddSqlRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, SqlUserRepository>();
            services.AddScoped<IBoardRepository, SqlBoardRepository>();
            services.AddScoped<IForumThreadRepository, SqlForumThreadRepository>();
            services.AddScoped<IPostRepository, SqlPostRepository>();
            services.AddScoped<IMessageRepository, SqlMessageRepository>();
            services.AddScoped<IReportRepository, SqlReportRepository>();

            return services;
        }
    }
}
