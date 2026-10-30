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
        private readonly IBoardRepository _boardRepository;
        private readonly IUserRepository _userRepository;

        public PostsController(
            IPostRepository postRepository,
            IForumThreadRepository threadRepository,
            IBoardRepository boardRepository,
            IUserRepository userRepository)
        {
            _postRepository = postRepository;
            _threadRepository = threadRepository;
            _boardRepository = boardRepository;
            _userRepository = userRepository;
        }

        [HttpGet("thread/{threadId}")]
        public ActionResult<IEnumerable<PostResponse>> GetThreadPosts(int threadId)
        {
            var thread = _threadRepository.GetById(threadId);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            var posts = _postRepository.GetByThreadId(threadId);
            return Ok(posts.Select(p => MapToPostResponse(p)));
        }

        // Used by a member's profile page to show their most recent activity.
        [HttpGet("by-user/{userId}")]
        public ActionResult<IEnumerable<PostResponse>> GetPostsByUser(int userId, [FromQuery] int take = 5)
        {
            take = Math.Clamp(take, 1, 50);
            var posts = _postRepository.GetAll()
                .Where(p => !p.IsDeleted && p.CreatedByUserId == userId.ToString())
                .OrderByDescending(p => p.CreatedAt)
                .Take(take);

            return Ok(posts.Select(p => MapToPostResponse(p)));
        }

        [HttpGet("{id}")]
        public ActionResult<PostResponse> GetPost(int id)
        {
            var post = _postRepository.GetById(id);
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            return Ok(MapToPostResponse(post));
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
        public ActionResult<PostResponse> CreatePost(CreatePostRequest request)
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
                CreatedAt = DateTime.UtcNow,
                QuotedPostId = request.QuotedPostId
            };

            post = _postRepository.Add(post);

            thread.LastPostAt = post.CreatedAt;
            thread.LastPostByUserId = userId;
            thread.PostCount++;
            _threadRepository.Update(thread);

            var board = _boardRepository.GetById(thread.BoardId);
            if (board != null)
            {
                board.PostCount++;
                board.LastPostAt = post.CreatedAt;
                board.LastPostByUserId = userId;
                _boardRepository.Update(board);
            }

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, MapToPostResponse(post));
        }

        [HttpPut("{id}")]
        [Authorize]
        public ActionResult<PostResponse> UpdatePost(int id, UpdatePostRequest request)
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

            return Ok(MapToPostResponse(post));
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

        private PostResponse MapToPostResponse(Post p)
        {
            return new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                ThreadId = p.ThreadId,
                CreatedByUserId = p.CreatedByUserId,
                CreatedByUsername = GetUsername(p.CreatedByUserId),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                UpdatedByUserId = p.UpdatedByUserId,
                UpdatedByUsername = GetUsername(p.UpdatedByUserId),
                IsDeleted = p.IsDeleted,
                DeletedByUserId = p.DeletedByUserId,
                DeletedByUsername = GetUsername(p.DeletedByUserId),
                DeletedAt = p.DeletedAt,
                DeleteReason = p.DeleteReason,
                IsEdited = p.IsEdited,
                EditReason = p.EditReason ?? string.Empty,
                QuotedPostId = p.QuotedPostId,
                // QuotedPost is a plain property, never populated by the repository
                // layer - resolve it from QuotedPostId instead.
                QuotedPost = p.QuotedPostId.HasValue
                    ? MapQuotedPost(_postRepository.GetById(p.QuotedPostId.Value))
                    : null
            };
        }

        // One level deep only - avoids re-resolving the quoted post's own quote
        // chain and the infinite recursion that would invite.
        private PostResponse? MapQuotedPost(Post? p)
        {
            if (p == null)
            {
                return null;
            }

            return new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                ThreadId = p.ThreadId,
                CreatedByUserId = p.CreatedByUserId,
                CreatedByUsername = GetUsername(p.CreatedByUserId),
                CreatedAt = p.CreatedAt,
                IsDeleted = p.IsDeleted,
                IsEdited = p.IsEdited,
                EditReason = p.EditReason ?? string.Empty
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
