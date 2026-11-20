using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;

namespace MvcBB.App.Controllers
{
    /// <summary>Admin page for toggling mods on/off - see IModsService / MvcBB.API's ModsController.</summary>
    [Authorize(Policy = "RequireAdmin")]
    public class ModsController : Controller
    {
        private readonly IModsService _modsService;

        public ModsController(IModsService modsService)
        {
            _modsService = modsService;
        }

        public async Task<IActionResult> Index()
        {
            var mods = await _modsService.GetAllAsync();
            return View(mods);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetEnabled(string id, bool enabled)
        {
            await _modsService.SetEnabledAsync(id, enabled);
            return RedirectToAction(nameof(Index));
        }
    }
}
