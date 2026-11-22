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
        private readonly IContentVisibilityService _visibility;

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
            IEmailSender emailSender,
            IContentVisibilityService visibility)
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
            _visibility = visibility;
        }

        [AllowAnonymous]
        [HttpGet("thread/{threadId}")]
        public ActionResult<IEnumerable<PostResponse>> GetThreadPosts(int threadId)
        {
            var thread = _threadRepository.GetById(threadId);
            if (!_visibility.CanSeeThread(thread))
            {
                return NotFound(new { message = "Thread not found" });
            }

            var posts = _postRepository.GetByThreadId(threadId);
            return Ok(MapToPostResponses(posts));
        }

        // Used by a member's profile page to show their most recent activity.
        [AllowAnonymous]
        [HttpGet("by-user/{userId}")]
        public ActionResult<IEnumerable<PostResponse>> GetPostsByUser(int userId, [FromQuery] int take = 5)
        {
            take = Math.Clamp(take, 1, 50);

            // Same board rules as everywhere else: guests don't get posts from
            // private boards, and only admins get posts from inactive boards.
            var posts = _postRepository.GetByUserId(
                userId,
                take,
                includePrivateBoards: User.Identity?.IsAuthenticated == true,
                includeInactiveBoards: User.IsInRole("Administrator"));

            return Ok(MapToPostResponses(posts.Where(p => _visibility.CanSeeThread(p.ThreadId)).ToList()));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public ActionResult<PostResponse> GetPost(int id)
        {
            var post = _postRepository.GetById(id);
            if (post == null || !_visibility.CanSeeThread(post.ThreadId))
            {
                return NotFound(new { message = "Post not found" });
            }

            return Ok(MapToPostResponse(post));
        }

        [AllowAnonymous]
        [HttpGet("{id}/quote")]
        public ActionResult<string> GetQuoteContent(int id)
        {
            var post = _postRepository.GetById(id);
            if (post == null || !_visibility.CanSeeThread(post.ThreadId))
            {
                return NotFound();
            }

            // A deleted post can't be quoted (its content would leak back out
            // through the reply form) unless the caller is staff.
            if (!_visibility.CanSeePostContent(post))
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
            if (post == null || !_visibility.CanSeeThread(post.ThreadId))
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
            if (!_visibility.CanSeeThread(thread))
            {
                return NotFound(new { message = "Thread not found" });
            }

            // A quote has to be a post the caller can actually see, and from
            // this same thread - otherwise quoting could copy a deleted post,
            // or a post from a private board, into a public thread.
            if (request.QuotedPostId.HasValue)
            {
                var quoted = _postRepository.GetById(request.QuotedPostId.Value);
                if (quoted == null || quoted.ThreadId != thread!.Id || !_visibility.CanSeePostContent(quoted))
                {
                    return BadRequest(new { message = "The quoted post isn't available." });
                }
            }

            if (thread!.IsLocked)
            {
                return BadRequest(new { message = "Thread is locked" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Checked before adding the post itself, otherwise this post
            // would always "find" itself and never count as a new participant.
            var isNewParticipant = !_postRepository.HasPostInThread(thread.Id, userId);

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
            if (isNewParticipant)
            {
                thread.ParticipantCount++;
            }
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
            if (post == null || !_visibility.CanSeeThread(post.ThreadId))
            {
                return NotFound(new { message = "Post not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            // A deleted post stays deleted - only staff can still edit it.
            if (post.IsDeleted && !_visibility.IsStaff)
            {
                return NotFound(new { message = "Post not found" });
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

        private PostResponse MapToPostResponse(Post p) => MapToPostResponses(new[] { p }).Single();

        /// <summary>
        /// Maps a page's worth of posts to responses with a fixed, small
        /// number of batched queries total. Username/avatar/post-count/role
        /// are already on Post via vw_PostDetails/vw_post_details (no query
        /// needed at all for those); like counts, "liked by me", quoted posts
        /// and the CustomTitle-vs-rank decision are batched across the whole
        /// page instead of costing several round trips per post.
        /// </summary>
        private List<PostResponse> MapToPostResponses(IReadOnlyList<Post> posts)
        {
            if (posts.Count == 0)
            {
                return new List<PostResponse>();
            }

            var postIds = posts.Select(p => p.Id).ToList();
            var likeCounts = _postLikeRepository.GetLikeCounts(postIds);
            var likedPostIds = CurrentUserId != null
                ? _postLikeRepository.GetLikedPostIds(postIds, CurrentUserId)
                : new HashSet<int>();

            var quotedIds = posts.Where(p => p.QuotedPostId.HasValue).Select(p => p.QuotedPostId!.Value).Distinct().ToList();
            var quotedPosts = _postRepository.GetByIds(quotedIds);

            // CustomTitle isn't part of vw_PostDetails (it's an admin-only
            // override the rest of the app rarely needs alongside a post) -
            // batch-fetch just the distinct authors on this page for it.
            var creatorIds = posts
                .Select(p => p.CreatedByUserId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => int.TryParse(id, out var cid) ? cid : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            var creators = _userRepository.GetByIds(creatorIds);
            var ranks = _rankRepository.GetAll();

            return posts.Select(p =>
            {
                var (rankTitle, rankImagePath) = GetDisplayRank(p.CreatedByUserId, p.UserPostCount, creators, ranks);

                PostResponse? quotedResponse = null;
                if (p.QuotedPostId.HasValue && quotedPosts.TryGetValue(p.QuotedPostId.Value, out var quoted))
                {
                    quotedResponse = MapQuotedPost(quoted, _visibility.CanSeePostContent(quoted));
                }

                // A deleted post keeps its place in the thread, but only staff
                // get its content back.
                var showContent = !p.IsDeleted || _visibility.IsStaff;

                return new PostResponse
                {
                    Id = p.Id,
                    Content = showContent ? p.Content : string.Empty,
                    ThreadId = p.ThreadId,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedByUsername = p.CreatedByUsername,
                    CreatedByAvatarUrl = p.UserAvatar,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    UpdatedByUserId = p.UpdatedByUserId,
                    UpdatedByUsername = p.UpdatedByUsername,
                    IsDeleted = p.IsDeleted,
                    DeletedByUserId = p.DeletedByUserId,
                    DeletedByUsername = p.DeletedByUsername,
                    DeletedAt = p.DeletedAt,
                    DeleteReason = p.DeleteReason,
                    IsEdited = p.IsEdited,
                    EditReason = p.EditReason ?? string.Empty,
                    ImageUrl = showContent ? p.ImageUrl : null,
                    QuotedPostId = p.QuotedPostId,
                    QuotedPost = quotedResponse,
                    IsImportedData = p.IsImportedData,
                    LikeCount = likeCounts.TryGetValue(p.Id, out var lc) ? lc : 0,
                    IsLikedByCurrentUser = likedPostIds.Contains(p.Id),
                    CreatedByRankTitle = rankTitle,
                    CreatedByRankImagePath = rankImagePath,
                    CreatedByRole = p.UserRole,
                    CreatedBySignature = p.UserSignature
                };
            }).ToList();
        }

        // An admin-set CustomTitle replaces the computed rank entirely (no
        // badge image for a custom title - that's only ever a rank's own
        // image). postCount comes from Post.UserPostCount (already computed
        // by the view/hydration) rather than another per-user count query.
        private static (string? Title, string? ImagePath) GetDisplayRank(
            string? userId,
            int postCount,
            IReadOnlyDictionary<int, MvcBB.Shared.Models.User.User> creators,
            IReadOnlyList<MvcBB.Shared.Models.Rank.RankModel> ranks)
        {
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id) || !creators.TryGetValue(id, out var user))
            {
                return (null, null);
            }

            if (!string.IsNullOrEmpty(user.CustomTitle))
            {
                return (user.CustomTitle, null);
            }

            var rank = RankCalculator.GetRank(ranks, postCount);
            return (rank?.Title, rank?.ImagePath);
        }

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // One level deep only - avoids re-resolving the quoted post's own quote
        // chain and the infinite recursion that would invite.
        // showContent is false when the quoted post has since been deleted or
        // sits somewhere the caller can't see - the quote box then stays, but
        // empty, instead of echoing the hidden text.
        private static PostResponse? MapQuotedPost(Post? p, bool showContent)
        {
            if (p == null)
            {
                return null;
            }

            return new PostResponse
            {
                Id = p.Id,
                Content = showContent ? p.Content : string.Empty,
                ThreadId = p.ThreadId,
                CreatedByUserId = p.CreatedByUserId,
                CreatedByUsername = p.CreatedByUsername,
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
