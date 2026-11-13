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
            "id, username::text AS username, password_hash, email::text AS email, created_at, last_login_at, role, signature, bio, avatar_url, show_email, custom_title, must_change_password, locked_until, last_login_ip";

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
                "CALL public.sp_user_insert(@Username, @PasswordHash, @Email, @Role, @Signature, @Bio, @AvatarUrl, @ShowEmail, @CustomTitle, @MustChangePassword, @LockedUntil, @LastLoginIp, NULL)",
                new
                {
                    user.Username,
                    user.PasswordHash,
                    user.Email,
                    Role = (short)user.Role,
                    user.Signature,
                    user.Bio,
                    user.AvatarUrl,
                    user.ShowEmail,
                    user.CustomTitle,
                    user.MustChangePassword,
                    user.LockedUntil,
                    user.LastLoginIp
                });

            return connection.QuerySingle<User>($"SELECT {SelectColumns} FROM public.fn_user_get_by_id(@Id)", new { Id = newId });
        }

        public void Update(User user)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_user_update(@Id, @Username, @PasswordHash, @Email, @LastLoginAt, @Role, @Signature, @Bio, @AvatarUrl, @ShowEmail, @CustomTitle, @MustChangePassword, @LockedUntil, @LastLoginIp)",
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
                    user.ShowEmail,
                    user.CustomTitle,
                    user.MustChangePassword,
                    user.LockedUntil,
                    user.LastLoginIp
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

        public IReadOnlyDictionary<int, User> GetByIds(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToArray();
            if (idList.Length == 0)
            {
                return new Dictionary<int, User>();
            }

            // A variable-length IN-list doesn't fit a stored function's fixed
            // parameters - this batch method goes straight against the table.
            using var connection = _connectionFactory.CreateConnection();
            var users = connection.Query<User>($"SELECT {SelectColumns} FROM public.users WHERE id = ANY(@Ids)", new { Ids = idList });
            return users.ToDictionary(u => u.Id);
        }

        public IReadOnlyDictionary<int, string> GetUsernamesByIds(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToArray();
            if (idList.Length == 0)
            {
                return new Dictionary<int, string>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<UsernameRow>(
                "SELECT id, username::text AS username FROM public.users WHERE id = ANY(@Ids)",
                new { Ids = idList });
            return rows.ToDictionary(r => r.Id, r => r.Username);
        }

        private class UsernameRow
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
        }

        public IReadOnlyDictionary<string, int> GetPostCountsByUsernames(IEnumerable<string> usernames)
        {
            var nameList = usernames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (nameList.Length == 0)
            {
                return new Dictionary<string, int>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<UsernameCountRow>(
                @"SELECT u.username::text AS username, COUNT(p.id) AS count
                  FROM public.users u
                  LEFT JOIN public.posts p ON p.created_by_user_id = u.id AND p.is_deleted = FALSE
                  WHERE u.username = ANY(@Usernames)
                  GROUP BY u.username",
                new { Usernames = nameList });
            return rows.ToDictionary(r => r.Username, r => r.Count, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, int> GetThreadCountsByUsernames(IEnumerable<string> usernames)
        {
            var nameList = usernames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (nameList.Length == 0)
            {
                return new Dictionary<string, int>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<UsernameCountRow>(
                @"SELECT u.username::text AS username, COUNT(t.id) AS count
                  FROM public.users u
                  LEFT JOIN public.threads t ON t.created_by_user_id = u.id
                  WHERE u.username = ANY(@Usernames)
                  GROUP BY u.username",
                new { Usernames = nameList });
            return rows.ToDictionary(r => r.Username, r => r.Count, StringComparer.OrdinalIgnoreCase);
        }

        private class UsernameCountRow
        {
            public string Username { get; set; } = string.Empty;
            public int Count { get; set; }
        }
    }
}
