using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.InMemory
{
    public class InMemorySmilieRepository : ISmilieRepository
    {
        private readonly InMemoryStore _store;

        public InMemorySmilieRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<SmilieModel> GetAll() => _store.Smilies.OrderBy(s => s.SortOrder).ToList();

        public SmilieModel? GetById(int id) => _store.Smilies.FirstOrDefault(s => s.Id == id);

        public SmilieModel Add(SmilieModel smilie)
        {
            if (smilie.Id == 0)
                smilie.Id = _store.Smilies.Count == 0 ? 1 : _store.Smilies.Max(s => s.Id) + 1;
            _store.Smilies.Add(smilie);
            return smilie;
        }

        public void Update(SmilieModel smilie)
        {
            var index = _store.Smilies.FindIndex(s => s.Id == smilie.Id);
            if (index >= 0)
                _store.Smilies[index] = smilie;
        }

        public void Remove(SmilieModel smilie) => _store.Smilies.Remove(smilie);
    }
}
