using Microsoft.Extensions.DependencyInjection;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.PostgreSql
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers PostgreSQL-backed implementations of all forum repositories.
        /// Add a Npgsql data source/DbContext and connection string configuration when implementing.
        /// </summary>
        public static IServiceCollection AddPostgreSqlRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, PostgreSqlUserRepository>();
            services.AddScoped<IBoardRepository, PostgreSqlBoardRepository>();
            services.AddScoped<IForumThreadRepository, PostgreSqlForumThreadRepository>();
            services.AddScoped<IPostRepository, PostgreSqlPostRepository>();
            services.AddScoped<IMessageRepository, PostgreSqlMessageRepository>();
            services.AddScoped<IReportRepository, PostgreSqlReportRepository>();

            return services;
        }
    }
}
