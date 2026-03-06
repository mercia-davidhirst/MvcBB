using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Interfaces;
using System.Security.Claims;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly IPostRepository _postRepository;
        private readonly IForumThreadRepository _threadRepository;

        public PostsController(IPostRepository postRepository, IForumThreadRepository threadRepository)
        {
            _postRepository = postRepository;
            _threadRepository = threadRepository;
        }

        [HttpGet("thread/{threadId}")]
        public ActionResult<IEnumerable<Post>> GetThreadPosts(int threadId)
        {
            var thread = _threadRepository.GetById(threadId);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            var posts = _postRepository.GetByThreadId(threadId);
            return Ok(posts);
        }

        [HttpGet("{id}")]
        public ActionResult<Post> GetPost(int id)
        {
            var post = _postRepository.GetById(id);
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            return Ok(post);
        }

        [HttpGet("{id}/quote")]
        public ActionResult<string> GetQuoteContent(int id)
        {
            var post = _postRepository.GetById(id);
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

            var thread = _threadRepository.GetById(request.ThreadId);
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
                Content = request.Content,
                ThreadId = request.ThreadId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            post = _postRepository.Add(post);

            thread.LastPostAt = post.CreatedAt;
            thread.LastPostByUserId = userId;
            thread.PostCount++;
            _threadRepository.Update(thread);

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

            var post = _postRepository.GetById(id);
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            if (post.CreatedByUserId != userId &&
                !User.IsInRole("Administrator") &&
                !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            var thread = _threadRepository.GetById(post.ThreadId);
            if (thread?.IsLocked == true)
            {
                return BadRequest(new { message = "Thread is locked" });
            }

            post.Content = request.Content;
            post.UpdatedAt = DateTime.UtcNow;
            post.UpdatedByUserId = userId;
            _postRepository.Update(post);

            return Ok(post);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult DeletePost(int id, string reason)
        {
            var post = _postRepository.GetById(id);
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
            _postRepository.Update(post);

            return Ok(new { message = "Post deleted successfully" });
        }
    }
}
