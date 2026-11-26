using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Moderation;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.ForumThread;
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
        private readonly IUserRepository _userRepository;
        private readonly IPostLikeRepository _postLikeRepository;
        private readonly IRankRepository _rankRepository;
        private readonly IWordFilterRepository _wordFilterRepository;
        private readonly IModerationQueueRepository _moderationQueue;
        private readonly IForumPostingService _posting;
        private readonly INotificationService _notifications;
        private readonly IModerationLogger _moderationLog;
        private readonly IContentVisibilityService _visibility;
        private readonly IThreadReadRepository _threadReadRepository;
        private readonly IDisplaySettingsService _displaySettings;

        public PostsController(
            IPostRepository postRepository,
            IForumThreadRepository threadRepository,
            IUserRepository userRepository,
            IPostLikeRepository postLikeRepository,
            IRankRepository rankRepository,
            IWordFilterRepository wordFilterRepository,
            IModerationQueueRepository moderationQueue,
            IForumPostingService posting,
            INotificationService notifications,
            IModerationLogger moderationLog,
            IContentVisibilityService visibility,
            IThreadReadRepository threadReadRepository,
            IDisplaySettingsService displaySettings)
        {
            _threadReadRepository = threadReadRepository;
            _displaySettings = displaySettings;
            _postRepository = postRepository;
            _threadRepository = threadRepository;
            _userRepository = userRepository;
            _postLikeRepository = postLikeRepository;
            _rankRepository = rankRepository;
            _wordFilterRepository = wordFilterRepository;
            _moderationQueue = moderationQueue;
            _posting = posting;
            _notifications = notifications;
            _moderationLog = moderationLog;
            _visibility = visibility;
        }

        /// <summary>
        /// One page of a discussion. Which page: the one holding postId when
        /// given (a link to a particular post); else, with unread=true, the
        /// one holding the caller's first unread post; else the last page
        /// with last=true; else page (default 1). Out-of-range pages are
        /// clamped. pageSize defaults to Admin > Settings' "Posts per page".
        /// </summary>
        [AllowAnonymous]
        [HttpGet("thread/{threadId}/page")]
        public ActionResult<ThreadPostPage> GetThreadPostPage(
            int threadId,
            [FromQuery] int? page = null,
            [FromQuery] int? postId = null,
            [FromQuery] bool unread = false,
            [FromQuery] bool last = false,
            [FromQuery] int? pageSize = null)
        {
            var thread = _threadRepository.GetById(threadId);
            if (!_visibility.CanSeeThread(thread))
            {
                return NotFound(new { message = "Thread not found" });
            }

            var size = Math.Clamp(pageSize ?? _displaySettings.GetDisplaySettings().PostsPerPage, 5, 100);

            // The caller's first unread post (signed in, with a saved read position).
            ThreadPostLocation? unreadLocation = null;
            if (int.TryParse(CurrentUserId, out var readerId)
                && _threadReadRepository.GetByThreadAndUser(threadId, CurrentUserId!) is { } readState)
            {
                unreadLocation = _postRepository.GetThreadLocation(threadId, unreadAfter: readState.LastReadPostAt, excludeUserId: readerId);
            }

            var target = postId.HasValue ? _postRepository.GetThreadLocation(threadId, postId: postId) : null;
            var total = (target ?? unreadLocation ?? _postRepository.GetThreadLocation(threadId)).TotalCount;
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size));

            int PageOf(int position) => position / size + 1;

            int pageNumber;
            if (target?.Position is { } targetPosition)
            {
                pageNumber = PageOf(targetPosition);
            }
            else if (unread && unreadLocation?.Position is { } unreadPosition)
            {
                pageNumber = PageOf(unreadPosition);
            }
            else if (last)
            {
                pageNumber = totalPages;
            }
            else
            {
                pageNumber = page ?? 1;
            }
            pageNumber = Math.Clamp(pageNumber, 1, totalPages);

            var posts = _postRepository.GetThreadPage(threadId, (pageNumber - 1) * size, size);

            return Ok(new ThreadPostPage
            {
                Posts = MapToPostResponses(posts),
                Page = pageNumber,
                PageSize = size,
                TotalPosts = total,
                FirstUnreadPostId = unreadLocation?.PostId,
                FirstUnreadPage = unreadLocation?.Position is { } p ? PageOf(p) : null
            });
        }

        /// <summary>
        /// Every post in a discussion at once. Kept for anything that relied
        /// on it before v1.4.0; the App pages discussions with
        /// GET thread/{threadId}/page instead, as a long one can have tens of
        /// thousands of posts.
        /// </summary>
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
                if (int.TryParse(userId, out var likerId))
                {
                    _notifications.NotifyLike(post, likerId);
                }
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
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            // A new member's reply waits for a moderator (see ModerationSettings).
            // It isn't a post yet, so nothing else changes until it's approved.
            if (_posting.ShouldHold(parsedUserId, _visibility.IsStaff))
            {
                var item = _moderationQueue.Add(new ModerationQueueItem
                {
                    Kind = QueuedPostKind.Reply,
                    UserId = parsedUserId,
                    BoardId = thread.BoardId,
                    ThreadId = thread.Id,
                    Content = request.Content,
                    QuotedPostId = request.QuotedPostId,
                    ImageUrl = request.ImageUrl
                });
                return StatusCode(StatusCodes.Status202Accepted, new HeldForApprovalResponse { QueueItemId = item.Id });
            }

            var post = await _posting.CreateReplyAsync(parsedUserId, thread, request.Content, request.QuotedPostId, request.ImageUrl);

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

            if (thread != null)
            {
                _notifications.NotifyEditedPost(thread, post);
            }

            if (post.CreatedByUserId != userId)
            {
                _moderationLog.Log(ModerationActions.EditPost, "Post", post.Id, ModerationLogger.ParseUserId(post.CreatedByUserId),
                    $"Edited a post in \"{thread?.Title ?? post.ThreadTitle}\"", request.EditReason);
            }

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

            _moderationLog.Log(ModerationActions.DeletePost, "Post", post.Id, ModerationLogger.ParseUserId(post.CreatedByUserId),
                $"Deleted a post in \"{post.ThreadTitle}\"", reason);

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

            // Every @name on the page (posts and the posts they quote), checked
            // against the member list in one query, so the page can link the
            // real ones. Names come back as stored, keyed case-insensitively.
            var mentionsByPost = posts.ToDictionary(
                p => p.Id,
                p => MentionParser.Extract(p.Content, includeQuotedText: true)
                    .Concat(p.QuotedPostId.HasValue && quotedPosts.TryGetValue(p.QuotedPostId.Value, out var q)
                        ? MentionParser.Extract(q.Content, includeQuotedText: true)
                        : Array.Empty<string>())
                    .ToList());
            var allMentioned = mentionsByPost.Values.SelectMany(v => v).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var realMembers = allMentioned.Count == 0
                ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                : _userRepository.GetIdsByUsernames(allMentioned);
            var storedNames = realMembers.Keys.ToDictionary(k => k, k => k, StringComparer.OrdinalIgnoreCase);

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
                var mentioned = mentionsByPost[p.Id]
                    .Select(name => storedNames.TryGetValue(name, out var stored) ? stored : null)
                    .Where(name => name != null)
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

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
                    CreatedBySignature = p.UserSignature,
                    MentionedUsernames = mentioned
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
    }
}
