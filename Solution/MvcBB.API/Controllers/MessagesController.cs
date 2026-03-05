using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Message;
using System.Security.Claims;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private static readonly List<MessageResponse> _messages = new();

        [HttpGet]
        public ActionResult<IEnumerable<MessageResponse>> GetMessages(bool? unreadOnly = false)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var query = _messages.Where(m => m.RecipientUserId == userId && !m.IsRead);
            if (!unreadOnly.GetValueOrDefault())
            {
                query = _messages.Where(m => 
                    m.RecipientUserId == userId || 
                    m.SenderUserId == userId);
            }

            return Ok(query.OrderByDescending(m => m.CreatedAt));
        }

        [HttpGet("{id}")]
        public ActionResult<MessageResponse> GetMessage(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var message = _messages.FirstOrDefault(m => m.Id == id);
            if (message == null)
            {
                return NotFound(new { message = "Message not found" });
            }

            if (message.RecipientUserId != userId && message.SenderUserId != userId)
            {
                return Forbid();
            }

            // Mark as read if recipient is viewing
            if (message.RecipientUserId == userId && !message.IsRead)
            {
                message.ReadAt = DateTime.UtcNow;
            }

            return Ok(message);
        }

        [HttpPost]
        public ActionResult<MessageResponse> CreateMessage(CreateMessageRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var message = new MessageResponse
            {
                Id = _messages.Count + 1,
                Subject = request.Subject,
                Content = request.Content,
                SenderUserId = userId,
                RecipientUserId = request.RecipientUserId,
                CreatedAt = DateTime.UtcNow
            };

            _messages.Add(message);

            return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, message);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteMessage(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var message = _messages.FirstOrDefault(m => m.Id == id);
            if (message == null)
            {
                return NotFound(new { message = "Message not found" });
            }

            if (message.RecipientUserId != userId && message.SenderUserId != userId)
            {
                return Forbid();
            }

            _messages.Remove(message);
            return Ok(new { message = "Message deleted successfully" });
        }

        [HttpGet("unread-count")]
        public ActionResult<int> GetUnreadCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var unreadCount = _messages.Count(m => 
                m.RecipientUserId == userId && 
                !m.IsRead);

            return Ok(new { unreadCount });
        }
    }
} 