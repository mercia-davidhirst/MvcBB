using MvcBB.Shared.Models.Board;

namespace MvcBB.App.Interfaces
{
    public interface IBoardService
    {
        Task<IEnumerable<BoardResponse>> GetBoardsAsync();
        Task<BoardResponse> GetBoardAsync(int id);
        Task<BoardResponse> CreateBoardAsync(CreateBoardRequest request);
        Task UpdateBoardAsync(int id, UpdateBoardRequest request);
        Task DeleteBoardAsync(int id);
    }
} 