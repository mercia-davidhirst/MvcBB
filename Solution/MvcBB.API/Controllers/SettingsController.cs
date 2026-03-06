using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Settings;
using MvcBB.API.Services;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly IDisplaySettingsService _displaySettingsService;

        public SettingsController(IDisplaySettingsService displaySettingsService)
        {
            _displaySettingsService = displaySettingsService;
        }

        [HttpGet("display")]
        public ActionResult<DisplaySettings> GetDisplaySettings()
        {
            return Ok(_displaySettingsService.GetDisplaySettings());
        }

        [Authorize(Roles = "Administrator,ApiClient")]
        [HttpPut("display")]
        public ActionResult PutDisplaySettings([FromBody] DisplaySettings settings)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _displaySettingsService.UpdateDisplaySettings(settings);
            return NoContent();
        }
    }
}
