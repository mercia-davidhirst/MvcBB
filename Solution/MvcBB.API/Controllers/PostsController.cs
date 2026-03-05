using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Post;
using System.Security.Claims;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private static readonly List<Post> _posts = new();
        private static readonly List<ForumThread> _threads = new();

        [HttpGet("thread/{threadId}")]
        public ActionResult<IEnumerable<Post>> GetThreadPosts(int threadId)
        {
            var thread = _threads.FirstOrDefault(t => t.Id == threadId);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            var posts = _posts
                .Where(p => p.ThreadId == threadId)
                .OrderBy(p => p.CreatedAt)
                .ToList();

            return Ok(posts);
        }

        [HttpGet("{id}")]
        public ActionResult<Post> GetPost(int id)
        {
            var post = _posts.FirstOrDefault(p => p.Id == id);
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            return Ok(post);
        }

        [HttpGet("{id}/quote")]
        public ActionResult<string> GetQuoteContent(int id)
        {
            var post = _posts.FirstOrDefault(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            var quotedContent = $"[quote={post.CreatedByUserId}]{post.Content}[/quote]\n\n";
            return Ok(new { quotedContent });
        }

        [HttpPost]
        [Authorize]
        public ActionResult<Post> CreatePost(CreatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var thread = _threads.FirstOrDefault(t => t.Id == request.ThreadId);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            if (thread.IsLocked)
            {
                return BadRequest(new { message = "Thread is locked" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var post = new Post
            {
                Id = _posts.Count + 1,
                Content = request.Content,
                ThreadId = request.ThreadId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _posts.Add(post);

            // Update thread's last post info
            thread.LastPostAt = post.CreatedAt;
            thread.LastPostByUserId = userId;
            thread.PostCount++;

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
        }

        [HttpPut("{id}")]
        [Authorize]
        public ActionResult<Post> UpdatePost(int id, UpdatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var post = _posts.FirstOrDefault(p => p.Id == id);
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Check if user is the post author or has moderator/admin role
            if (post.CreatedByUserId != userId && 
                !User.IsInRole("Administrator") && 
                !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            var thread = _threads.FirstOrDefault(t => t.Id == post.ThreadId);
            if (thread?.IsLocked == true)
            {
                return BadRequest(new { message = "Thread is locked" });
            }

            post.Content = request.Content;
            post.UpdatedAt = DateTime.UtcNow;
            post.UpdatedByUserId = userId;

            return Ok(post);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult DeletePost(int id, string reason)
        {
            var post = _posts.FirstOrDefault(p => p.Id == id);
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            post.IsDeleted = true;
            post.DeletedByUserId = userId;
            post.DeletedAt = DateTime.UtcNow;
            post.DeleteReason = reason;

            return Ok(new { message = "Post deleted successfully" });
        }
    }
} 