using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.SQL
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers SQL Server-backed implementations of all forum repositories,
        /// reading the connection string from ConnectionStrings:SqlServer.
        /// </summary>
        public static IServiceCollection AddSqlRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SqlServer")
                ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is not configured");

            services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));

            services.AddScoped<IUserRepository, SqlUserRepository>();
            services.AddScoped<IBoardRepository, SqlBoardRepository>();
            services.AddScoped<IForumThreadRepository, SqlForumThreadRepository>();
            services.AddScoped<IPostRepository, SqlPostRepository>();
            services.AddScoped<IMessageRepository, SqlMessageRepository>();
            services.AddScoped<IReportRepository, SqlReportRepository>();
            services.AddScoped<IBBCodeTagRepository, SqlBBCodeTagRepository>();
            services.AddScoped<ISmilieRepository, SqlSmilieRepository>();

            return services;
        }
    }
}
