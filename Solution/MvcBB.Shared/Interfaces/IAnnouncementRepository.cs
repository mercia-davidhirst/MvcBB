using MvcBB.Shared.Models.Announcement;

namespace MvcBB.Shared.Interfaces
{
    public interface IAnnouncementRepository
    {
        IReadOnlyList<AnnouncementModel> GetAll();

        /// <summary>
        /// Announcements that are IsActive and currently within their optional
        /// StartDate/EndDate scheduling window, ordered by SortOrder - what the
        /// portal page and any other public-facing surface should display.
        /// </summary>
        IReadOnlyList<AnnouncementModel> GetActive();

        AnnouncementModel? GetById(int id);
        AnnouncementModel Add(AnnouncementModel announcement);
        void Update(AnnouncementModel announcement);
        void Remove(AnnouncementModel announcement);
    }
}
