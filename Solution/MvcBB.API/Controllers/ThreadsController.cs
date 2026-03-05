using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Board;
using System.Security.Claims;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThreadsController : ControllerBase
    {
        private static readonly List<ForumThread> _threads = new();
        private static readonly List<Board> _boards = new();

        [HttpGet("board/{boardId}")]
        public ActionResult<IEnumerable<ForumThread>> GetBoardThreads(int boardId)
        {
            var board = _boards.FirstOrDefault(b => b.Id == boardId);
            if (board == null)
            {
                return NotFound(new { message = "Board not found" });
            }

            var threads = _threads
                .Where(t => t.BoardId == boardId)
                .OrderByDescending(t => t.IsSticky)
                .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt)
                .ToList();

            return Ok(threads);
        }

        [HttpGet("{id}")]
        public ActionResult<ForumThread> GetThread(int id)
        {
            var thread = _threads.FirstOrDefault(t => t.Id == id);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            // Increment view count
            thread.ViewCount++;

            return Ok(thread);
        }

        [HttpPost]
        [Authorize]
        public ActionResult<ForumThread> CreateThread(CreateForumThreadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = _boards.FirstOrDefault(b => b.Id == request.BoardId);
            if (board == null)
            {
                return NotFound(new { message = "Board not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var thread = new ForumThread
            {
                Id = _threads.Count + 1,
                Title = request.Title,
                BoardId = request.BoardId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastPostAt = DateTime.UtcNow,
                LastPostByUserId = userId
            };

            _threads.Add(thread);

            // Update board statistics
            board.ThreadCount++;
            board.LastPostAt = thread.CreatedAt;
            board.LastPostByUserId = userId;

            return CreatedAtAction(nameof(GetThread), new { id = thread.Id }, thread);
        }

        [HttpPut("{id}")]
        [Authorize]
        public ActionResult<ForumThread> UpdateThread(int id, UpdateForumThreadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var thread = _threads.FirstOrDefault(t => t.Id == id);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Check if user is the thread author or has moderator/admin role
            if (thread.CreatedByUserId != userId && 
                !User.IsInRole("Administrator") && 
                !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            thread.Title = request.Title;
            thread.IsSticky = request.IsSticky;
            thread.IsLocked = request.IsLocked;
            thread.UpdatedAt = DateTime.UtcNow;

            return Ok(thread);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult DeleteThread(int id)
        {
            var thread = _threads.FirstOrDefault(t => t.Id == id);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            var board = _boards.FirstOrDefault(b => b.Id == thread.BoardId);
            if (board != null)
            {
                board.ThreadCount--;
            }

            _threads.Remove(thread);
            return Ok(new { message = "Thread deleted successfully" });
        }
    }
} 