using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL-backed user repository. Replace with EF Core or Dapper implementation.
    /// </summary>
    public class SqlUserRepository : IUserRepository
    {
        public IReadOnlyList<User> GetAll() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public User? GetById(int id) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public User? GetByUsername(string username) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public User? GetByEmail(string email) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public User Add(User user) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Update(User user) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public bool ExistsWithUsername(string username, int? excludeUserId = null) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public bool ExistsWithEmail(string email, int? excludeUserId = null) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public int CountPostsByUsername(string username) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public int CountThreadsByUsername(string username) => throw new NotImplementedException("Implement with EF Core or Dapper");
    }
}
