using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.InMemory
{
    public class InMemoryBBCodeTagRepository : IBBCodeTagRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryBBCodeTagRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<BBCodeTagModel> GetAll() => _store.BBCodeTags.OrderBy(t => t.SortOrder).ToList();

        public BBCodeTagModel? GetById(int id) => _store.BBCodeTags.FirstOrDefault(t => t.Id == id);

        public BBCodeTagModel Add(BBCodeTagModel tag)
        {
            if (tag.Id == 0)
                tag.Id = _store.BBCodeTags.Count == 0 ? 1 : _store.BBCodeTags.Max(t => t.Id) + 1;
            _store.BBCodeTags.Add(tag);
            return tag;
        }

        public void Update(BBCodeTagModel tag)
        {
            var index = _store.BBCodeTags.FindIndex(t => t.Id == tag.Id);
            if (index >= 0)
                _store.BBCodeTags[index] = tag;
        }

        public void Remove(BBCodeTagModel tag) => _store.BBCodeTags.Remove(tag);
    }
}
