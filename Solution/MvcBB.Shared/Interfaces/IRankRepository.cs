using MvcBB.Shared.Models.Rank;

namespace MvcBB.Shared.Interfaces
{
    public interface IRankRepository
    {
        IReadOnlyList<RankModel> GetAll();
        RankModel? GetById(int id);
        RankModel Add(RankModel rank);
        void Update(RankModel rank);
        void Remove(RankModel rank);
    }
}
