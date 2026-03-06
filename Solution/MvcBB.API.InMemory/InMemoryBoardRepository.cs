using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Board;

namespace MvcBB.API.InMemory
{
    public class InMemoryBoardRepository : IBoardRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryBoardRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<Board> GetAll() => _store.Boards.OrderBy(b => b.SortOrder).ToList();

        public Board? GetById(int id) => _store.Boards.FirstOrDefault(b => b.Id == id);

        public Board Add(Board board)
        {
            if (board.Id == 0)
                board.Id = _store.Boards.Count == 0 ? 1 : _store.Boards.Max(b => b.Id) + 1;
            _store.Boards.Add(board);
            return board;
        }

        public void Update(Board board)
        {
            var index = _store.Boards.FindIndex(b => b.Id == board.Id);
            if (index >= 0)
                _store.Boards[index] = board;
        }

        public void Remove(Board board) => _store.Boards.Remove(board);
    }
}
