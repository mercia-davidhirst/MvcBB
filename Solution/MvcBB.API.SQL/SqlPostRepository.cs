using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Post;

namespace MvcBB.API.SQL
{
    public class SqlPostRepository : IPostRepository
    {
        public IReadOnlyList<Post> GetAll() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public IReadOnlyList<Post> GetByThreadId(int threadId) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public Post? GetById(int id) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public Post Add(Post post) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Update(Post post) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Remove(Post post) => throw new NotImplementedException("Implement with EF Core or Dapper");
    }
}
