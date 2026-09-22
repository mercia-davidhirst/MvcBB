using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlForumThreadRepository : IForumThreadRepository
    {
        public IReadOnlyList<ForumThread> GetAll() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public IReadOnlyList<ForumThread> GetByBoardId(int boardId) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public ForumThread? GetById(int id) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public ForumThread Add(ForumThread thread) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Update(ForumThread thread) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Remove(ForumThread thread) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
    }
}
