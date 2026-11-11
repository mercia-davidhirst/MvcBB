using MvcBB.Shared.Models.WordFilter;

namespace MvcBB.App.Interfaces
{
    /// <summary>Async client for the word-filter management endpoints on MvcBB.API.</summary>
    public interface IWordFilterService
    {
        Task<IEnumerable<WordFilterModel>> GetWordFiltersAsync();
        Task<WordFilterModel?> GetWordFilterAsync(int id);
        Task AddWordFilterAsync(WordFilterModel model);
        Task UpdateWordFilterAsync(int id, WordFilterModel model);
        Task DeleteWordFilterAsync(int id);
    }
}
