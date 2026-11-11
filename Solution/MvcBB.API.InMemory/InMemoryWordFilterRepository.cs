using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.WordFilter;

namespace MvcBB.API.InMemory
{
    public class InMemoryWordFilterRepository : IWordFilterRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryWordFilterRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<WordFilterModel> GetAll() => _store.WordFilters.OrderBy(f => f.SortOrder).ToList();

        public WordFilterModel? GetById(int id) => _store.WordFilters.FirstOrDefault(f => f.Id == id);

        public WordFilterModel Add(WordFilterModel filter)
        {
            if (filter.Id == 0)
                filter.Id = _store.WordFilters.Count == 0 ? 1 : _store.WordFilters.Max(f => f.Id) + 1;
            _store.WordFilters.Add(filter);
            return filter;
        }

        public void Update(WordFilterModel filter)
        {
            var index = _store.WordFilters.FindIndex(f => f.Id == filter.Id);
            if (index >= 0)
                _store.WordFilters[index] = filter;
        }

        public void Remove(WordFilterModel filter) => _store.WordFilters.Remove(filter);
    }
}
