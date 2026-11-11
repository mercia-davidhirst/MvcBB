using MvcBB.Shared.Models.Announcement;

namespace MvcBB.App.Interfaces
{
    public interface IAnnouncementService
    {
        Task<IEnumerable<AnnouncementModel>> GetAnnouncementsAsync();

        /// <summary>
        /// Active announcements currently within their scheduling window, ordered
        /// by SortOrder - what the portal page and any other public-facing
        /// surface should display.
        /// </summary>
        Task<IEnumerable<AnnouncementModel>> GetActiveAnnouncementsAsync();

        Task<AnnouncementModel?> GetAnnouncementAsync(int id);
        Task AddAnnouncementAsync(AnnouncementModel model);
        Task UpdateAnnouncementAsync(int id, AnnouncementModel model);
        Task DeleteAnnouncementAsync(int id);
    }
}
