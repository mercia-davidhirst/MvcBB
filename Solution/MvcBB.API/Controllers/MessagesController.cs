using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Message;
using MvcBB.Shared.Interfaces;
using System.Security.Claims;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;

        public MessagesController(IMessageRepository messageRepository, IUserRepository userRepository)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<MessageResponse>> GetMessages([FromQuery] string folder = "inbox", [FromQuery] bool unreadOnly = false)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            IEnumerable<Message> messages;
            if (unreadOnly)
            {
                messages = _messageRepository.GetByRecipientUserId(userId, unreadOnly: true);
            }
            else if (string.Equals(folder, "sent", StringComparison.OrdinalIgnoreCase))
            {
                messages = _messageRepository.GetByRecipientOrSenderUserId(userId)
                    .Where(m => m.SenderUserId == userId);
            }
            else
            {
                messages = _messageRepository.GetByRecipientUserId(userId);
            }

            return Ok(messages.Select(MapToResponse));
        }

        [HttpGet("{id}")]
        public ActionResult<MessageResponse> GetMessage(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var message = _messageRepository.GetById(id);
            if (message == null)
            {
                return NotFound(new { message = "Message not found" });
            }

            if (message.RecipientUserId != userId && message.SenderUserId != userId)
            {
                return Forbid();
            }

            if (message.RecipientUserId == userId && !message.ReadAt.HasValue)
            {
                message.ReadAt = DateTime.UtcNow;
                _messageRepository.Update(message);
            }

            return Ok(MapToResponse(message));
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

            if (request.RecipientUserId == userId)
            {
                return BadRequest(new { message = "You can't send a message to yourself." });
            }

            if (!int.TryParse(request.RecipientUserId, out var recipientId) || _userRepository.GetById(recipientId) == null)
            {
                return BadRequest(new { message = "Recipient not found." });
            }

            var message = new Message
            {
                Subject = request.Subject,
                Content = request.Content,
                SenderUserId = userId,
                RecipientUserId = request.RecipientUserId,
                CreatedAt = DateTime.UtcNow
            };

            message = _messageRepository.Add(message);

            return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, MapToResponse(message));
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteMessage(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var message = _messageRepository.GetById(id);
            if (message == null)
            {
                return NotFound(new { message = "Message not found" });
            }

            if (message.RecipientUserId != userId && message.SenderUserId != userId)
            {
                return Forbid();
            }

            _messageRepository.Remove(message);
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

            var unreadCount = _messageRepository.GetUnreadCountByRecipientUserId(userId);

            return Ok(new { unreadCount });
        }

        private MessageResponse MapToResponse(Message m)
        {
            return new MessageResponse
            {
                Id = m.Id,
                Subject = m.Subject,
                Content = m.Content,
                SenderUserId = m.SenderUserId,
                RecipientUserId = m.RecipientUserId,
                CreatedAt = m.CreatedAt,
                ReadAt = m.ReadAt,
                SenderUsername = GetUsername(m.SenderUserId),
                RecipientUsername = GetUsername(m.RecipientUserId)
            };
        }

        private string GetUsername(string? userId)
        {
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var id))
            {
                return _userRepository.GetById(id)?.Username ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
