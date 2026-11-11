using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Announcement;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementManagementService _announcementService;

        public AnnouncementsController(IAnnouncementManagementService announcementService)
        {
            _announcementService = announcementService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<AnnouncementModel>> GetAnnouncements()
        {
            return Ok(_announcementService.GetAnnouncements());
        }

        [HttpGet("active")]
        public ActionResult<IEnumerable<AnnouncementModel>> GetActiveAnnouncements()
        {
            return Ok(_announcementService.GetActiveAnnouncements());
        }

        [HttpGet("{id}")]
        public ActionResult<AnnouncementModel> GetAnnouncement(int id)
        {
            var announcement = _announcementService.GetAnnouncement(id);
            if (announcement == null)
            {
                return NotFound(new { message = "Announcement not found" });
            }

            return Ok(announcement);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public ActionResult<AnnouncementModel> CreateAnnouncement(AnnouncementModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = _announcementService.AddAnnouncement(model);
            return CreatedAtAction(nameof(GetAnnouncement), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public ActionResult<AnnouncementModel> UpdateAnnouncement(int id, AnnouncementModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_announcementService.GetAnnouncement(id) == null)
            {
                return NotFound(new { message = "Announcement not found" });
            }

            _announcementService.UpdateAnnouncement(id, model);
            return Ok(model);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public IActionResult DeleteAnnouncement(int id)
        {
            if (_announcementService.GetAnnouncement(id) == null)
            {
                return NotFound(new { message = "Announcement not found" });
            }

            _announcementService.DeleteAnnouncement(id);
            return NoContent();
        }
    }
}
