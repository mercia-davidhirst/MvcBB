using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.API.Services;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Notification;

namespace MvcBB.API.Controllers
{
    /// <summary>
    /// The signed-in member's own notification centre. Every call works on
    /// the caller's notifications only.
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationRepository _notifications;
        private readonly IContentVisibilityService _visibility;

        public NotificationsController(INotificationRepository notifications, IContentVisibilityService visibility)
        {
            _notifications = notifications;
            _visibility = visibility;
        }

        /// <summary>
        /// A page of the caller's notifications, newest first. One about a
        /// discussion that has since gone, or that sits on a board the caller
        /// can no longer see, stays in the list (so it can still be marked
        /// read) but loses its link and its extract.
        /// </summary>
        [HttpGet]
        public ActionResult<NotificationPage> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        {
            if (CurrentUserId is not { } userId)
            {
                return Unauthorized(new { message = "User not found" });
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var (items, total) = _notifications.GetByUser(userId, (page - 1) * pageSize, pageSize);

            return Ok(new NotificationPage
            {
                Notifications = items.Select(HideIfGone).ToList(),
                TotalCount = total,
                UnreadCount = _notifications.CountUnread(userId),
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("unread-count")]
        public ActionResult GetUnreadCount()
        {
            if (CurrentUserId is not { } userId)
            {
                return Unauthorized(new { message = "User not found" });
            }

            return Ok(new { unreadCount = _notifications.CountUnread(userId) });
        }

        /// <summary>Marks one notification read and returns it, so the App can send the member on to what it's about.</summary>
        [HttpPost("{id:int}/read")]
        public ActionResult<Notification> MarkRead(int id)
        {
            if (CurrentUserId is not { } userId)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var notification = _notifications.GetById(id);
            if (notification == null || notification.UserId != userId)
            {
                return NotFound(new { message = "Notification not found" });
            }

            _notifications.MarkRead(id, userId);
            notification.IsRead = true;

            // Nothing to send them to if the discussion has gone or is hidden from them now.
            return Ok(HideIfGone(notification));
        }

        [HttpPost("read-all")]
        public ActionResult MarkAllRead()
        {
            if (CurrentUserId is not { } userId)
            {
                return Unauthorized(new { message = "User not found" });
            }

            _notifications.MarkAllRead(userId);
            return NoContent();
        }

        private bool IsStillVisible(Notification n) =>
            !n.ThreadId.HasValue || (n.ThreadTitle != null && _visibility.CanSeeThread(n.ThreadId.Value));

        private Notification HideIfGone(Notification n)
        {
            if (!IsStillVisible(n))
            {
                n.ThreadId = null;
                n.PostId = null;
                n.ThreadTitle = null;
                if (n.Type != NotificationType.Moderation)
                {
                    n.Excerpt = null;
                }
            }
            return n;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
