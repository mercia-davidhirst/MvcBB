using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.SQL
{
    public class SqlForumThreadRepository : IForumThreadRepository
    {
        public IReadOnlyList<ForumThread> GetAll() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public IReadOnlyList<ForumThread> GetByBoardId(int boardId) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public ForumThread? GetById(int id) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public ForumThread Add(ForumThread thread) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Update(ForumThread thread) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Remove(ForumThread thread) => throw new NotImplementedException("Implement with EF Core or Dapper");
    }
}
