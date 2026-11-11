using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Rank;

namespace MvcBB.API.InMemory
{
    public class InMemoryRankRepository : IRankRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryRankRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<RankModel> GetAll() => _store.Ranks.OrderBy(r => r.SortOrder).ToList();

        public RankModel? GetById(int id) => _store.Ranks.FirstOrDefault(r => r.Id == id);

        public RankModel Add(RankModel rank)
        {
            if (rank.Id == 0)
                rank.Id = _store.Ranks.Count == 0 ? 1 : _store.Ranks.Max(r => r.Id) + 1;
            _store.Ranks.Add(rank);
            return rank;
        }

        public void Update(RankModel rank)
        {
            var index = _store.Ranks.FindIndex(r => r.Id == rank.Id);
            if (index >= 0)
                _store.Ranks[index] = rank;
        }

        public void Remove(RankModel rank) => _store.Ranks.Remove(rank);
    }
}
