using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Post;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlPostRepository : IPostRepository
    {
        public IReadOnlyList<Post> GetAll() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public IReadOnlyList<Post> GetByThreadId(int threadId) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public Post? GetById(int id) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public Post Add(Post post) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Update(Post post) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Remove(Post post) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
    }
}
