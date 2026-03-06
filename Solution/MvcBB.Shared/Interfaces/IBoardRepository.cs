using MvcBB.Shared.Models.Board;

namespace MvcBB.Shared.Interfaces
{
    public interface IBoardRepository
    {
        IReadOnlyList<Board> GetAll();
        Board? GetById(int id);
        Board Add(Board board);
        void Update(Board board);
        void Remove(Board board);
    }
}
