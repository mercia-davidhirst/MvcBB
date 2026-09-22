using MvcBB.Shared.Models.BBCode;

namespace MvcBB.App.Interfaces
{
    /// <summary>
    /// Async client for the BBCode tag/smilie management endpoints on MvcBB.API.
    /// Parsing/stripping/validating BBCode is unrelated to this - those stay
    /// synchronous and local via MvcBB.Shared.Interfaces.IBBCodeService
    /// (injected directly where needed), since they don't touch persisted data.
    /// </summary>
    public interface IMvcBBCodeService
    {
        Task<IEnumerable<BBCodeTagModel>> GetBBCodeTagsAsync();
        Task<BBCodeTagModel?> GetBBCodeTagAsync(int id);
        Task AddBBCodeTagAsync(BBCodeTagModel model);
        Task UpdateBBCodeTagAsync(int id, BBCodeTagModel model);
        Task DeleteBBCodeTagAsync(int id);

        Task<IEnumerable<SmilieModel>> GetSmiliesAsync();
        Task<SmilieModel?> GetSmilieAsync(int id);
        Task AddSmilieAsync(SmilieModel model);
        Task UpdateSmilieAsync(int id, SmilieModel model);
        Task DeleteSmilieAsync(int id);

        /// <summary>
        /// Gets a dictionary of active smilies where the key is the smilie code
        /// and the value is the HTML representation, ordered by SortOrder.
        /// </summary>
        Task<Dictionary<string, string>> GetAvailableSmiliesAsync();
    }
}
