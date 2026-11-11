using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Announcement;

namespace MvcBB.API.InMemory
{
    public class InMemoryAnnouncementRepository : IAnnouncementRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryAnnouncementRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<AnnouncementModel> GetAll() => _store.Announcements.OrderBy(a => a.SortOrder).ToList();

        public IReadOnlyList<AnnouncementModel> GetActive()
        {
            var now = DateTime.UtcNow;
            return _store.Announcements
                .Where(a => a.IsActive
                    && (a.StartDate == null || a.StartDate <= now)
                    && (a.EndDate == null || a.EndDate >= now))
                .OrderBy(a => a.SortOrder)
                .ToList();
        }

        public AnnouncementModel? GetById(int id) => _store.Announcements.FirstOrDefault(a => a.Id == id);

        public AnnouncementModel Add(AnnouncementModel announcement)
        {
            if (announcement.Id == 0)
                announcement.Id = _store.Announcements.Count == 0 ? 1 : _store.Announcements.Max(a => a.Id) + 1;
            _store.Announcements.Add(announcement);
            return announcement;
        }

        public void Update(AnnouncementModel announcement)
        {
            var index = _store.Announcements.FindIndex(a => a.Id == announcement.Id);
            if (index >= 0)
                _store.Announcements[index] = announcement;
        }

        public void Remove(AnnouncementModel announcement) => _store.Announcements.Remove(announcement);
    }
}
