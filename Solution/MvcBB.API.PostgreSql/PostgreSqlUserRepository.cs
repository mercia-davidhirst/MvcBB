using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed user repository, calling the fn_user_*/sp_user_*
    /// functions/procedures in Database/PostGresSql. username/email are
    /// CITEXT columns; reads explicitly cast them to text so they map onto
    /// User.Username/Email without depending on Npgsql's citext plugin being
    /// registered.
    /// </summary>
    public class PostgreSqlUserRepository : IUserRepository
    {
        private const string SelectColumns =
            "id, username::text AS username, password_hash, email::text AS email, created_at, last_login_at, role, signature, bio, avatar_url, show_email";

        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlUserRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<User> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<User>($"SELECT {SelectColumns} FROM public.fn_user_get_all()").ToList();
        }

        public User? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<User>($"SELECT {SelectColumns} FROM public.fn_user_get_by_id(@Id)", new { Id = id });
        }

        public User? GetByUsername(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<User>($"SELECT {SelectColumns} FROM public.fn_user_get_by_username(@Username)", new { Username = username });
        }

        public User? GetByEmail(string email)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<User>($"SELECT {SelectColumns} FROM public.fn_user_get_by_email(@Email)", new { Email = email });
        }

        public User Add(User user)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_user_insert(@Username, @PasswordHash, @Email, @Role, @Signature, @Bio, @AvatarUrl, @ShowEmail, NULL)",
                new
                {
                    user.Username,
                    user.PasswordHash,
                    user.Email,
                    Role = (short)user.Role,
                    user.Signature,
                    user.Bio,
                    user.AvatarUrl,
                    user.ShowEmail
                });

            return connection.QuerySingle<User>($"SELECT {SelectColumns} FROM public.fn_user_get_by_id(@Id)", new { Id = newId });
        }

        public void Update(User user)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_user_update(@Id, @Username, @PasswordHash, @Email, @LastLoginAt, @Role, @Signature, @Bio, @AvatarUrl, @ShowEmail)",
                new
                {
                    user.Id,
                    user.Username,
                    user.PasswordHash,
                    user.Email,
                    user.LastLoginAt,
                    Role = (short)user.Role,
                    user.Signature,
                    user.Bio,
                    user.AvatarUrl,
                    user.ShowEmail
                });
        }

        public bool ExistsWithUsername(string username, int? excludeUserId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>("SELECT public.fn_user_exists_with_username(@Username, @ExcludeUserId)", new { Username = username, ExcludeUserId = excludeUserId });
        }

        public bool ExistsWithEmail(string email, int? excludeUserId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>("SELECT public.fn_user_exists_with_email(@Email, @ExcludeUserId)", new { Email = email, ExcludeUserId = excludeUserId });
        }

        public int CountPostsByUsername(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_user_count_posts_by_username(@Username)", new { Username = username });
        }

        public int CountThreadsByUsername(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_user_count_threads_by_username(@Username)", new { Username = username });
        }
    }
}
