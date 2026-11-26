using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Moderation;

namespace MvcBB.App.Controllers
{
    /// <summary>
    /// Moderation: the queue of posts held for approval and the moderation
    /// log (Administrators and Moderators), and the settings for holding new
    /// members' posts (Administrators only). Linked from the Admin Dashboard,
    /// the navbar's Admin dropdown (plus a navbar icon for moderators) and the
    /// admin sidebar.
    /// </summary>
    [Authorize(Roles = "Administrator,Moderator")]
    public class ModerationController : Controller
    {
        private readonly IModerationService _moderation;
        private readonly IMvcBBCodeService _bbCodeService;
        private readonly IImageUploadService _imageUploadService;
        private readonly ILogger<ModerationController> _logger;

        public ModerationController(
            IModerationService moderation,
            IMvcBBCodeService bbCodeService,
            IImageUploadService imageUploadService,
            ILogger<ModerationController> logger)
        {
            _moderation = moderation;
            _bbCodeService = bbCodeService;
            _imageUploadService = imageUploadService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var queue = await _moderation.GetQueueAsync();
                ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
                ViewData["Settings"] = await _moderation.GetSettingsAsync();
                return View(queue);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogError(ex, "Failed to load the moderation queue");
                TempData["Error"] = "Failed to load the moderation queue.";
                return View(Array.Empty<ModerationQueueItem>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var result = await _moderation.ApproveAsync(id);
                TempData["Success"] = "Approved - the post is now live.";
                TempData["ApprovedThreadId"] = result.ThreadId;
                TempData["ApprovedPostId"] = result.PostId;
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogWarning(ex, "Failed to approve queued post {QueueItemId}", id);
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            try
            {
                var item = await _moderation.RejectAsync(id, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim());

                // The image was saved when the member posted; nothing uses it now.
                _imageUploadService.DeleteImage(item.ImageUrl);
                TempData["Success"] = $"Turned down {item.Username}'s post. They've been told.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogWarning(ex, "Failed to turn down queued post {QueueItemId}", id);
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Log(int page = 1)
        {
            try
            {
                return View(await _moderation.GetLogAsync(Math.Max(1, page)));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogError(ex, "Failed to load the moderation log");
                TempData["Error"] = "Failed to load the moderation log.";
                return View(new ModerationLogPage());
            }
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Settings()
        {
            try
            {
                return View(await _moderation.GetSettingsAsync());
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogError(ex, "Failed to load moderation settings");
                TempData["Error"] = "Failed to load the moderation settings.";
                return View(new ModerationSettings());
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(ModerationSettings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _moderation.UpdateSettingsAsync(model);
                TempData["Success"] = "Moderation settings saved.";
                return RedirectToAction(nameof(Settings));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogError(ex, "Failed to save moderation settings");
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        // Polled by site.js for the navbar's moderation queue badge.
        [HttpGet]
        public async Task<IActionResult> QueueCount()
        {
            try
            {
                return Json(new { count = await _moderation.GetQueueCountAsync() });
            }
            catch (Exception)
            {
                return Json(new { count = 0 });
            }
        }
    }
}
