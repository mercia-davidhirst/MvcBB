using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed user repository. Replace with Npgsql or EF Core (Npgsql provider) implementation.
    /// </summary>
    public class PostgreSqlUserRepository : IUserRepository
    {
        public IReadOnlyList<User> GetAll() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public User? GetById(int id) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public User? GetByUsername(string username) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public User? GetByEmail(string email) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public User Add(User user) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Update(User user) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public bool ExistsWithUsername(string username, int? excludeUserId = null) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public bool ExistsWithEmail(string email, int? excludeUserId = null) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public int CountPostsByUsername(string username) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public int CountThreadsByUsername(string username) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
    }
}
