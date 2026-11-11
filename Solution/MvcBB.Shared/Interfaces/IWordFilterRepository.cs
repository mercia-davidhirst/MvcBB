using MvcBB.Shared.Models.WordFilter;

namespace MvcBB.Shared.Interfaces
{
    public interface IWordFilterRepository
    {
        IReadOnlyList<WordFilterModel> GetAll();
        WordFilterModel? GetById(int id);
        WordFilterModel Add(WordFilterModel filter);
        void Update(WordFilterModel filter);
        void Remove(WordFilterModel filter);
    }
}
