using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed user repository, calling the usp_User_* stored
    /// procedures in Database/SQL/Stored Procedures. All Users columns match
    /// the User model 1:1 (no string/int userId translation needed here).
    /// </summary>
    public class SqlUserRepository : IUserRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlUserRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<User> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<User>("dbo.usp_User_GetAll", commandType: CommandType.StoredProcedure).ToList();
        }

        public User? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<User>("dbo.usp_User_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public User? GetByUsername(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<User>("dbo.usp_User_GetByUsername", new { Username = username }, commandType: CommandType.StoredProcedure);
        }

        public User? GetByEmail(string email)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<User>("dbo.usp_User_GetByEmail", new { Email = email }, commandType: CommandType.StoredProcedure);
        }

        public User Add(User user)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<User>("dbo.usp_User_Insert", new
            {
                user.Username,
                user.PasswordHash,
                user.Email,
                user.CreatedAt,
                user.Role,
                user.Signature,
                user.Bio,
                user.AvatarUrl,
                user.ShowEmail,
                user.CustomTitle,
                user.MustChangePassword,
                user.LockedUntil,
                user.LastLoginIp
            }, commandType: CommandType.StoredProcedure);
        }

        public void Update(User user)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_User_Update", new
            {
                user.Id,
                user.Username,
                user.PasswordHash,
                user.Email,
                user.LastLoginAt,
                user.Role,
                user.Signature,
                user.Bio,
                user.AvatarUrl,
                user.ShowEmail,
                user.CustomTitle,
                user.MustChangePassword,
                user.LockedUntil,
                user.LastLoginIp
            }, commandType: CommandType.StoredProcedure);
        }

        public bool ExistsWithUsername(string username, int? excludeUserId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>("dbo.usp_User_ExistsWithUsername", new { Username = username, ExcludeUserId = excludeUserId }, commandType: CommandType.StoredProcedure);
        }

        public bool ExistsWithEmail(string email, int? excludeUserId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>("dbo.usp_User_ExistsWithEmail", new { Email = email, ExcludeUserId = excludeUserId }, commandType: CommandType.StoredProcedure);
        }

        public int CountPostsByUsername(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_User_CountPostsByUsername", new { Username = username }, commandType: CommandType.StoredProcedure);
        }

        public int CountThreadsByUsername(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_User_CountThreadsByUsername", new { Username = username }, commandType: CommandType.StoredProcedure);
        }

        public IReadOnlyDictionary<int, User> GetByIds(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0)
            {
                return new Dictionary<int, User>();
            }

            // A variable-length IN-list doesn't fit a stored procedure's fixed
            // parameters - this batch method goes straight against the table.
            // The id list is passed as a single JSON array parameter (see
            // SqlListParameter) instead of Dapper's "IN @Ids" expansion (one
            // SQL parameter per id), which risks SQL Server's
            // 2100-parameter-per-query ceiling once a list gets large enough.
            using var connection = _connectionFactory.CreateConnection();
            var users = connection.Query<User>(
                "SELECT * FROM dbo.Users WHERE Id IN (SELECT CAST(value AS INT) FROM OPENJSON(@Ids))",
                new { Ids = SqlListParameter.ToJson(idList) });
            return users.ToDictionary(u => u.Id);
        }

        public IReadOnlyDictionary<int, string> GetUsernamesByIds(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<UsernameRow>(
                "SELECT Id, Username FROM dbo.Users WHERE Id IN (SELECT CAST(value AS INT) FROM OPENJSON(@Ids))",
                new { Ids = SqlListParameter.ToJson(idList) });
            return rows.ToDictionary(r => r.Id, r => r.Username);
        }

        private class UsernameRow
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
        }

        public IReadOnlyDictionary<string, int> GetPostCountsByUsernames(IEnumerable<string> usernames)
        {
            var nameList = usernames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (nameList.Count == 0)
            {
                return new Dictionary<string, int>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<UsernameCountRow>(
                @"SELECT u.Username, COUNT(p.Id) AS Count
                  FROM dbo.Users u
                  LEFT JOIN dbo.Posts p ON p.CreatedByUserId = u.Id AND p.IsDeleted = 0
                  WHERE u.Username IN (SELECT value FROM OPENJSON(@Usernames))
                  GROUP BY u.Username",
                new { Usernames = SqlListParameter.ToJson(nameList) });
            return rows.ToDictionary(r => r.Username, r => r.Count, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, int> GetThreadCountsByUsernames(IEnumerable<string> usernames)
        {
            var nameList = usernames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (nameList.Count == 0)
            {
                return new Dictionary<string, int>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<UsernameCountRow>(
                @"SELECT u.Username, COUNT(t.Id) AS Count
                  FROM dbo.Users u
                  LEFT JOIN dbo.Threads t ON t.CreatedByUserId = u.Id
                  WHERE u.Username IN (SELECT value FROM OPENJSON(@Usernames))
                  GROUP BY u.Username",
                new { Usernames = SqlListParameter.ToJson(nameList) });
            return rows.ToDictionary(r => r.Username, r => r.Count, StringComparer.OrdinalIgnoreCase);
        }

        private class UsernameCountRow
        {
            public string Username { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public (IReadOnlyList<User> Items, int TotalCount) Search(string query, int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var totalCount = connection.ExecuteScalar<int>(
                "dbo.usp_Search_UsersCount",
                new { Query = query },
                commandType: CommandType.StoredProcedure);
            var rows = connection.Query<User>(
                "dbo.usp_Search_Users",
                new { Query = query, Skip = skip, Take = take },
                commandType: CommandType.StoredProcedure);
            return (rows.ToList(), totalCount);
        }

        public (IReadOnlyList<User> Items, int TotalCount) GetMembersPage(UserRole? role, string? search, string? sortBy, bool sortDescending, int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var totalCount = connection.ExecuteScalar<int>(
                "dbo.usp_User_GetMembersPageCount",
                new { Role = role, Search = search },
                commandType: CommandType.StoredProcedure);
            var rows = connection.Query<User>(
                "dbo.usp_User_GetMembersPage",
                new { Role = role, Search = search, SortBy = sortBy, SortDescending = sortDescending, Skip = skip, Take = take },
                commandType: CommandType.StoredProcedure);
            return (rows.ToList(), totalCount);
        }

        public (int TotalMembers, int ActiveMembers) GetStats(DateTime activeSince)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingle<StatsRow>(
                "dbo.usp_User_GetStats",
                new { ActiveSince = activeSince },
                commandType: CommandType.StoredProcedure);
            return (row.TotalMembers, row.ActiveMembers);
        }

        private class StatsRow
        {
            public int TotalMembers { get; set; }
            public int ActiveMembers { get; set; }
        }
    }
}
