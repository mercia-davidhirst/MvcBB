using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Announcement;

namespace MvcBB.API.Services
{
    public class AnnouncementManagementService : IAnnouncementManagementService
    {
        private readonly IAnnouncementRepository _announcementRepository;

        public AnnouncementManagementService(IAnnouncementRepository announcementRepository)
        {
            _announcementRepository = announcementRepository;
        }

        public IReadOnlyList<AnnouncementModel> GetAnnouncements() => _announcementRepository.GetAll();

        public IReadOnlyList<AnnouncementModel> GetActiveAnnouncements() => _announcementRepository.GetActive();

        public AnnouncementModel? GetAnnouncement(int id) => _announcementRepository.GetById(id);

        public AnnouncementModel AddAnnouncement(AnnouncementModel model)
        {
            model.Id = 0;
            model.CreatedAt = DateTime.UtcNow;
            return _announcementRepository.Add(model);
        }

        public void UpdateAnnouncement(int id, AnnouncementModel model)
        {
            var existing = _announcementRepository.GetById(id);
            if (existing == null)
            {
                return;
            }

            model.Id = id;
            model.CreatedAt = existing.CreatedAt;
            _announcementRepository.Update(model);
        }

        public void DeleteAnnouncement(int id)
        {
            var existing = _announcementRepository.GetById(id);
            if (existing != null)
            {
                _announcementRepository.Remove(existing);
            }
        }
    }
}
