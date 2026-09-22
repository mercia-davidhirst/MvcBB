using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Board;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlBoardRepository : IBoardRepository
    {
        public IReadOnlyList<Board> GetAll() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public Board? GetById(int id) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public Board Add(Board board) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Update(Board board) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Remove(Board board) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
    }
}
