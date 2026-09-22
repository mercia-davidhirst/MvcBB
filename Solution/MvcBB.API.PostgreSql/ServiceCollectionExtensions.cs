using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.PostgreSql
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers PostgreSQL-backed implementations of all forum repositories,
        /// reading the connection string from ConnectionStrings:PostgreSql.
        /// </summary>
        public static IServiceCollection AddPostgreSqlRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            // Postgres columns are snake_case (e.g. last_post_by_user_id) while the
            // shared models are PascalCase (LastPostByUserId); this makes Dapper
            // match them automatically instead of requiring column aliases everywhere.
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            // Npgsql maps "timestamp with time zone" columns to DateTimeOffset,
            // not DateTime - bridge that back to the plain DateTime every
            // Shared model uses. See PostgreSqlDateTimeHandler for details.
            var dateTimeHandler = new PostgreSqlDateTimeHandler();
            SqlMapper.AddTypeHandler(typeof(DateTime), dateTimeHandler);
            SqlMapper.AddTypeHandler(typeof(DateTime?), dateTimeHandler);

            var connectionString = configuration.GetConnectionString("PostgreSql")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is not configured");

            services.AddSingleton<IPostgreSqlConnectionFactory>(new PostgreSqlConnectionFactory(connectionString));

            services.AddScoped<IUserRepository, PostgreSqlUserRepository>();
            services.AddScoped<IBoardRepository, PostgreSqlBoardRepository>();
            services.AddScoped<IForumThreadRepository, PostgreSqlForumThreadRepository>();
            services.AddScoped<IPostRepository, PostgreSqlPostRepository>();
            services.AddScoped<IMessageRepository, PostgreSqlMessageRepository>();
            services.AddScoped<IReportRepository, PostgreSqlReportRepository>();
            services.AddScoped<IBBCodeTagRepository, PostgreSqlBBCodeTagRepository>();
            services.AddScoped<ISmilieRepository, PostgreSqlSmilieRepository>();

            return services;
        }
    }
}
