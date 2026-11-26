using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Notification;

namespace MvcBB.App.Controllers
{
    /// <summary>
    /// The notification centre: replies, @mentions, quotes, likes, private
    /// messages and moderation decisions, in one list.
    /// </summary>
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationsService _notifications;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationsService notifications, ILogger<NotificationsController> logger)
        {
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                return View(await _notifications.GetPageAsync(Math.Max(1, page)));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogError(ex, "Failed to load notifications");
                TempData["Error"] = "Failed to load your notifications. Please try again later.";
                return View(new NotificationPage());
            }
        }

        /// <summary>Marks the notification read and goes to what it's about.</summary>
        public async Task<IActionResult> Open(int id)
        {
            try
            {
                var notification = await _notifications.MarkReadAsync(id);
                if (notification == null)
                {
                    TempData["Error"] = "That notification is no longer available.";
                    return RedirectToAction(nameof(Index));
                }

                if (notification.MessageId.HasValue)
                {
                    return RedirectToAction("Details", "Messages", new { id = notification.MessageId.Value });
                }

                if (notification.ThreadId.HasValue)
                {
                    // With the post id the thread opens at whichever page holds it.
                    return notification.PostId.HasValue
                        ? Redirect(Url.Action("Details", "Threads", new { id = notification.ThreadId.Value, postId = notification.PostId.Value }) + $"#post-{notification.PostId.Value}")
                        : RedirectToAction("Details", "Threads", new { id = notification.ThreadId.Value });
                }

                if (notification.ThreadId == null && notification.Type != NotificationType.Moderation)
                {
                    TempData["Error"] = "The discussion this notification was about is no longer available.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogError(ex, "Failed to open notification {NotificationId}", id);
                TempData["Error"] = "Failed to open that notification.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                await _notifications.MarkAllReadAsync();
                TempData["Success"] = "All notifications marked as read.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex) when (ex is ServiceException or HttpRequestException)
            {
                _logger.LogError(ex, "Failed to mark notifications read");
                TempData["Error"] = "Failed to mark your notifications as read.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Polled by site.js for the navbar bell, like Messages/UnreadCount.
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                return Json(new { count = await _notifications.GetUnreadCountAsync() });
            }
            catch (Exception)
            {
                return Json(new { count = 0 });
            }
        }
    }
}
