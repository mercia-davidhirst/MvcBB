using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MvcBB.ModKit;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Mod;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModsController : ControllerBase
    {
        private readonly IModRepository _modRepository;
        private readonly IMemoryCache _cache;

        public ModsController(IModRepository modRepository, IMemoryCache cache)
        {
            _modRepository = modRepository;
            _cache = cache;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public ActionResult<IEnumerable<ModRecord>> GetAll()
        {
            return Ok(_modRepository.GetAll());
        }

        /// <summary>Anonymous - just the enabled Id list, polled/cached by both
        /// this process's own ModEnabledFilter and every MvcBB.App instance.</summary>
        [HttpGet("enabled")]
        public ActionResult<IEnumerable<string>> GetEnabledIds()
        {
            return Ok(_modRepository.GetAll().Where(m => m.IsEnabled).Select(m => m.Id));
        }

        [HttpPost("{id}/enable")]
        [Authorize(Roles = "Administrator")]
        public IActionResult Enable(string id) => SetEnabled(id, true);

        [HttpPost("{id}/disable")]
        [Authorize(Roles = "Administrator")]
        public IActionResult Disable(string id) => SetEnabled(id, false);

        private IActionResult SetEnabled(string id, bool enabled)
        {
            if (_modRepository.GetById(id) == null)
            {
                return NotFound(new { message = "Mod not found" });
            }

            _modRepository.SetEnabled(id, enabled);
            _cache.Remove(ModCacheKeys.EnabledModIds);
            return NoContent();
        }
    }
}
