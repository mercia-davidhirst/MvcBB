using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Board;

namespace MvcBB.API.SQL
{
    public class SqlBoardRepository : IBoardRepository
    {
        public IReadOnlyList<Board> GetAll() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public Board? GetById(int id) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public Board Add(Board board) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Update(Board board) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Remove(Board board) => throw new NotImplementedException("Implement with EF Core or Dapper");
    }
}
