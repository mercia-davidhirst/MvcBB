using MvcBB.Shared.Models.Rank;

namespace MvcBB.App.Interfaces
{
    /// <summary>Async client for the rank management endpoints on MvcBB.API.</summary>
    public interface IRankService
    {
        Task<IEnumerable<RankModel>> GetRanksAsync();
        Task<RankModel?> GetRankAsync(int id);
        Task AddRankAsync(RankModel model);
        Task UpdateRankAsync(int id, RankModel model);
        Task DeleteRankAsync(int id);
    }
}
