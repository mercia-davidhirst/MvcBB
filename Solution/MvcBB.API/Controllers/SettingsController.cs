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
        private readonly IPasswordPolicyService _passwordPolicyService;

        public SettingsController(IDisplaySettingsService displaySettingsService, IPasswordPolicyService passwordPolicyService)
        {
            _displaySettingsService = displaySettingsService;
            _passwordPolicyService = passwordPolicyService;
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

        // Public (not just Administrator/ApiClient) - a password form (register,
        // change password, etc.) needs to show the current requirements to an
        // ordinary signed-out visitor, not just an admin.
        [HttpGet("password-policy")]
        public ActionResult<PasswordPolicy> GetPasswordPolicy()
        {
            return Ok(_passwordPolicyService.GetPasswordPolicy());
        }

        [Authorize(Roles = "Administrator,ApiClient")]
        [HttpPut("password-policy")]
        public ActionResult PutPasswordPolicy([FromBody] PasswordPolicy policy)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _passwordPolicyService.UpdatePasswordPolicy(policy);
            return NoContent();
        }
    }
}
