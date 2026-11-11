using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.Message;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Services;
using MvcBB.API.Services;
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
        private readonly IPostLikeRepository _postLikeRepository;
        private readonly IRankRepository _rankRepository;
        private readonly IWordFilterRepository _wordFilterRepository;
        private readonly IThreadSubscriptionRepository _subscriptionRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IEmailSender _emailSender;

        public PostsController(
            IPostRepository postRepository,
            IForumThreadRepository threadRepository,
            IBoardRepository boardRepository,
            IUserRepository userRepository,
            IPostLikeRepository postLikeRepository,
            IRankRepository rankRepository,
            IWordFilterRepository wordFilterRepository,
            IThreadSubscriptionRepository subscriptionRepository,
            IMessageRepository messageRepository,
            IEmailSender emailSender)
        {
            _postRepository = postRepository;
            _threadRepository = threadRepository;
            _boardRepository = boardRepository;
            _userRepository = userRepository;
            _postLikeRepository = postLikeRepository;
            _rankRepository = rankRepository;
            _wordFilterRepository = wordFilterRepository;
            _subscriptionRepository = subscriptionRepository;
            _messageRepository = messageRepository;
            _emailSender = emailSender;
        }

        [HttpGet("thread/{threadId}")]
        public ActionResult<IEnumerable<PostResponse>> GetThreadPosts(int threadId)
        {
            var thread = _threadRepository.GetById(threadId);
            if (thread == null || !IsThreadVisible(thread.BoardId, User.Identity?.IsAuthenticated == true))
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
            var thread = post != null ? _threadRepository.GetById(post.ThreadId) : null;
            if (post == null || thread == null || !IsThreadVisible(thread.BoardId, User.Identity?.IsAuthenticated == true))
            {
                return NotFound(new { message = "Post not found" });
            }

            return Ok(MapToPostResponse(post));
        }

        private bool IsThreadVisible(int boardId, bool isAuthenticated)
        {
            var board = _boardRepository.GetById(boardId);
            return board == null || board.IsVisibleTo(isAuthenticated);
        }

        [HttpGet("{id}/quote")]
        public ActionResult<string> GetQuoteContent(int id)
        {
            var post = _postRepository.GetById(id);
            var thread = post != null ? _threadRepository.GetById(post.ThreadId) : null;
            if (post == null || thread == null || !IsThreadVisible(thread.BoardId, User.Identity?.IsAuthenticated == true))
            {
                return NotFound();
            }

            var quotedContent = $"[quote={post.CreatedByUserId}]{post.Content}[/quote]\n\n";
            return Ok(new { quotedContent });
        }

        [HttpPost("{id}/like")]
        [Authorize]
        public ActionResult ToggleLike(int id)
        {
            var post = _postRepository.GetById(id);
            var thread = post != null ? _threadRepository.GetById(post.ThreadId) : null;
            if (post == null || thread == null || !IsThreadVisible(thread.BoardId, User.Identity?.IsAuthenticated == true))
            {
                return NotFound(new { message = "Post not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            bool liked;
            if (_postLikeRepository.HasLiked(id, userId))
            {
                _postLikeRepository.RemoveLike(id, userId);
                liked = false;
            }
            else
            {
                _postLikeRepository.AddLike(id, userId);
                liked = true;
            }

            return Ok(new { liked, likeCount = _postLikeRepository.GetLikeCount(id) });
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PostResponse>> CreatePost(CreatePostRequest request)
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
                Content = WordFilterApplier.Apply(request.Content, _wordFilterRepository.GetAll()),
                ThreadId = request.ThreadId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                QuotedPostId = request.QuotedPostId,
                ImageUrl = request.ImageUrl
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

            await NotifySubscribersAsync(thread, post, userId);

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, MapToPostResponse(post));
        }

        // Fans out to everyone watching this thread except the replier
        // themselves - a subscription's two flags are independent, so a
        // watcher can get a PM, an email, both or (harmlessly) neither.
        private async Task NotifySubscribersAsync(ForumThread thread, Post post, string replierUserId)
        {
            var subscribers = _subscriptionRepository.GetByThreadId(thread.Id)
                .Where(s => s.UserId != replierUserId)
                .ToList();
            if (subscribers.Count == 0)
            {
                return;
            }

            var replierUsername = GetUsername(replierUserId);
            var subject = $"New reply in \"{thread.Title}\"";
            var body = $"{replierUsername} replied to a thread you're watching:\n\n{post.Content}";
            string? systemUserId = null;

            foreach (var subscription in subscribers)
            {
                if (subscription.NotifyViaMessage)
                {
                    systemUserId ??= SystemUserProvider.GetOrCreateSystemUserId(_userRepository);
                    _messageRepository.Add(new Message
                    {
                        Subject = subject,
                        Content = body,
                        SenderUserId = systemUserId,
                        RecipientUserId = subscription.UserId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (subscription.NotifyViaEmail && int.TryParse(subscription.UserId, out var recipientId))
                {
                    var recipient = _userRepository.GetById(recipientId);
                    if (recipient != null && !string.IsNullOrEmpty(recipient.Email))
                    {
                        await _emailSender.SendAsync(recipient.Email, subject, body);
                    }
                }
            }
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

            post.Content = WordFilterApplier.Apply(request.Content, _wordFilterRepository.GetAll());
            post.ImageUrl = request.ImageUrl;
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
            var (rankTitle, rankImagePath) = GetDisplayRank(p.CreatedByUserId);
            return new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                ThreadId = p.ThreadId,
                CreatedByUserId = p.CreatedByUserId,
                CreatedByUsername = GetUsername(p.CreatedByUserId),
                CreatedByAvatarUrl = GetAvatarUrl(p.CreatedByUserId),
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
                ImageUrl = p.ImageUrl,
                QuotedPostId = p.QuotedPostId,
                // QuotedPost is a plain property, never populated by the repository
                // layer - resolve it from QuotedPostId instead.
                QuotedPost = p.QuotedPostId.HasValue
                    ? MapQuotedPost(_postRepository.GetById(p.QuotedPostId.Value))
                    : null,
                LikeCount = _postLikeRepository.GetLikeCount(p.Id),
                IsLikedByCurrentUser = CurrentUserId != null && _postLikeRepository.HasLiked(p.Id, CurrentUserId),
                CreatedByRankTitle = rankTitle,
                CreatedByRankImagePath = rankImagePath
            };
        }

        // An admin-set CustomTitle replaces the computed rank entirely (no
        // badge image for a custom title - that's only ever a rank's own
        // image).
        private (string? Title, string? ImagePath) GetDisplayRank(string? userId)
        {
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
            {
                return (null, null);
            }

            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return (null, null);
            }

            if (!string.IsNullOrEmpty(user.CustomTitle))
            {
                return (user.CustomTitle, null);
            }

            var rank = RankCalculator.GetRank(_rankRepository.GetAll(), _userRepository.CountPostsByUsername(user.Username));
            return (rank?.Title, rank?.ImagePath);
        }

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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

        private string? GetAvatarUrl(string? userId)
        {
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var id))
            {
                return _userRepository.GetById(id)?.AvatarUrl;
            }
            return null;
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
