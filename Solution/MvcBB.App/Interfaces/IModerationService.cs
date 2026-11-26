using MvcBB.Shared.Models.Moderation;

namespace MvcBB.App.Interfaces
{
    /// <summary>The moderation queue, moderation log and settings (api/moderation).</summary>
    public interface IModerationService
    {
        Task<IReadOnlyList<ModerationQueueItem>> GetQueueAsync();
        Task<int> GetQueueCountAsync();

        /// <summary>Publishes a held post. Throws ServiceException with the API's reason if it can't.</summary>
        Task<ApproveQueuedPostResponse> ApproveAsync(int id);

        /// <summary>Turns down a held post and returns it (so its image can be deleted).</summary>
        Task<ModerationQueueItem> RejectAsync(int id, string? reason);

        Task<ModerationLogPage> GetLogAsync(int page = 1, int pageSize = 50);
        Task<ModerationSettings> GetSettingsAsync();
        Task UpdateSettingsAsync(ModerationSettings settings);
    }
}
