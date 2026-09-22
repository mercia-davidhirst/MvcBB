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
                user.ShowEmail
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
                user.ShowEmail
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
    }
}
