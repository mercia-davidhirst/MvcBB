using MvcBB.Shared.Models.Announcement;

namespace MvcBB.Shared.Interfaces
{
    public interface IAnnouncementManagementService
    {
        IReadOnlyList<AnnouncementModel> GetAnnouncements();
        IReadOnlyList<AnnouncementModel> GetActiveAnnouncements();
        AnnouncementModel? GetAnnouncement(int id);
        AnnouncementModel AddAnnouncement(AnnouncementModel model);
        void UpdateAnnouncement(int id, AnnouncementModel model);
        void DeleteAnnouncement(int id);
    }
}
