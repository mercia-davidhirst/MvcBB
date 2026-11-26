using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Poll;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.ThreadRead;
using MvcBB.Shared.Models.ThreadSubscription;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Services;
using System.Security.Claims;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThreadsController : ControllerBase
    {
        private readonly IForumThreadRepository _threadRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPollRepository _pollRepository;
        private readonly IWordFilterRepository _wordFilterRepository;
        private readonly IThreadSubscriptionRepository _subscriptionRepository;
        private readonly IThreadReadRepository _threadReadRepository;
        private readonly MvcBB.API.Services.IDisplaySettingsService _displaySettingsService;
        private readonly MvcBB.API.Services.IContentVisibilityService _visibility;
        private readonly MvcBB.API.Services.IForumPostingService _posting;
        private readonly MvcBB.API.Services.IModerationLogger _moderationLog;
        private readonly IModerationQueueRepository _moderationQueue;
        private readonly INotificationRepository _notificationRepository;

        public ThreadsController(
            IForumThreadRepository threadRepository,
            IBoardRepository boardRepository,
            IPostRepository postRepository,
            IUserRepository userRepository,
            IPollRepository pollRepository,
            IWordFilterRepository wordFilterRepository,
            IThreadSubscriptionRepository subscriptionRepository,
            IThreadReadRepository threadReadRepository,
            MvcBB.API.Services.IDisplaySettingsService displaySettingsService,
            MvcBB.API.Services.IContentVisibilityService visibility,
            MvcBB.API.Services.IForumPostingService posting,
            MvcBB.API.Services.IModerationLogger moderationLog,
            IModerationQueueRepository moderationQueue,
            INotificationRepository notificationRepository)
        {
            _posting = posting;
            _moderationLog = moderationLog;
            _moderationQueue = moderationQueue;
            _notificationRepository = notificationRepository;
            _threadRepository = threadRepository;
            _boardRepository = boardRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _pollRepository = pollRepository;
            _wordFilterRepository = wordFilterRepository;
            _subscriptionRepository = subscriptionRepository;
            _threadReadRepository = threadReadRepository;
            _displaySettingsService = displaySettingsService;
            _visibility = visibility;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<ThreadListResponse> GetThreads([FromQuery] int page = 1, [FromQuery] int? pageSize = null)
        {
            var displaySettings = _displaySettingsService.GetDisplaySettings();
            var effectivePageSize = pageSize ?? displaySettings.ThreadsPerPage;
            effectivePageSize = Math.Clamp(effectivePageSize, 1, 100);
            page = Math.Max(1, page);

            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var skip = (page - 1) * effectivePageSize;
            var (pageOfThreads, totalThreads) = _threadRepository.GetPage(skip, effectivePageSize, isAuthenticated);
            var totalPages = totalThreads == 0 ? 1 : (int)Math.Ceiling(totalThreads / (double)effectivePageSize);

            // The data layer already leaves out private boards for guests; this
            // also drops threads from boards an admin has deactivated.
            var visibleThreads = pageOfThreads.Where(_visibility.CanSeeThread).ToList();

            return Ok(new ThreadListResponse
            {
                Threads = MapToThreadResponses(visibleThreads),
                TotalThreads = totalThreads,
                Page = page,
                PageSize = effectivePageSize,
                TotalPages = totalPages
            });
        }

        [AllowAnonymous]
        [HttpGet("board/{boardId}")]
        public ActionResult<ThreadListResponse> GetBoardThreads(int boardId, [FromQuery] int page = 1, [FromQuery] int? pageSize = null)
        {
            var board = _boardRepository.GetById(boardId);
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            if (!_visibility.CanSeeBoard(board))
            {
                return NotFound(new { message = "Board not found" });
            }

            var displaySettings = _displaySettingsService.GetDisplaySettings();
            var effectivePageSize = pageSize ?? displaySettings.ThreadsPerPage;
            effectivePageSize = Math.Clamp(effectivePageSize, 1, 100);
            page = Math.Max(1, page);

            var skip = (page - 1) * effectivePageSize;
            var (pageOfThreads, totalThreads) = _threadRepository.GetPage(skip, effectivePageSize, isAuthenticated, boardId);
            var totalPages = totalThreads == 0 ? 1 : (int)Math.Ceiling(totalThreads / (double)effectivePageSize);

            return Ok(new ThreadListResponse
            {
                Threads = MapToThreadResponses(pageOfThreads),
                TotalThreads = totalThreads,
                Page = page,
                PageSize = effectivePageSize,
                TotalPages = totalPages
            });
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public ActionResult<ThreadResponse> GetThread(int id)
        {
            var thread = _threadRepository.GetById(id);
            if (thread == null || !_visibility.CanSeeThread(thread))
            {
                return NotFound(new { message = "Thread not found" });
            }

            thread.ViewCount++;
            _threadRepository.Update(thread);

            var response = MapToThreadResponses(new[] { thread }).Single();

            // Only the single-thread view needs the viewer's read position
            // (to open at their first unread post), so lists don't pay for it.
            var userId = CurrentUserId;
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var parsedUserId))
            {
                var read = _threadReadRepository.GetByThreadAndUser(id, userId);
                response.ReadState = new ThreadReadState
                {
                    LastReadPostId = read?.LastReadPostId,
                    LastReadPostAt = read?.LastReadPostAt,
                    AutoJumpToUnread = _userRepository.GetById(parsedUserId)?.AutoJumpToUnread ?? true
                };

                // Opening the discussion covers any notifications about it.
                try
                {
                    _notificationRepository.MarkReadForThread(parsedUserId, id);
                }
                catch (Exception)
                {
                    // Never let the notification centre stop a thread from opening.
                }
            }

            return Ok(response);
        }

        /// <summary>
        /// Records that the signed-in member has read the thread up to the
        /// given post. Only ever moves their position forward.
        /// </summary>
        [HttpPut("{id}/read")]
        [Authorize]
        public ActionResult MarkThreadRead(int id, MarkThreadReadRequest request)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var thread = _threadRepository.GetById(id);
            if (thread == null || !_visibility.CanSeeThread(thread))
            {
                return NotFound(new { message = "Thread not found" });
            }

            var post = _postRepository.GetById(request.PostId);
            if (post == null || post.ThreadId != id)
            {
                return NotFound(new { message = "Post not found in this thread" });
            }

            _threadReadRepository.MarkRead(id, userId, post.Id, post.CreatedAt);
            return NoContent();
        }

        [HttpPost]
        [Authorize]
        public ActionResult<ThreadResponse> CreateThread(CreateForumThreadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = _boardRepository.GetById(request.BoardId);
            if (board == null || !_visibility.CanSeeBoard(board))
            {
                return NotFound(new { message = "Board not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            // A new member's discussion waits for a moderator (see
            // ModerationSettings). It isn't a thread yet, so nothing else
            // changes until it's approved.
            if (_posting.ShouldHold(parsedUserId, _visibility.IsStaff))
            {
                var item = _moderationQueue.Add(new MvcBB.Shared.Models.Moderation.ModerationQueueItem
                {
                    Kind = MvcBB.Shared.Models.Moderation.QueuedPostKind.NewThread,
                    UserId = parsedUserId,
                    BoardId = board.Id,
                    Title = request.Title,
                    Content = request.Content,
                    PollJson = request.Poll == null ? null : System.Text.Json.JsonSerializer.Serialize(request.Poll)
                });
                return StatusCode(StatusCodes.Status202Accepted, new MvcBB.Shared.Models.Moderation.HeldForApprovalResponse { QueueItemId = item.Id });
            }

            var (thread, _) = _posting.CreateThread(parsedUserId, board, request.Title, request.Content, request.Poll);

            return CreatedAtAction(nameof(GetThread), new { id = thread.Id }, MapToThreadResponses(new[] { thread }).Single());
        }

        [HttpPost("{id}/poll/vote")]
        [Authorize]
        public ActionResult<PollResponse> VotePoll(int id, VotePollRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var thread = _threadRepository.GetById(id);
            if (!_visibility.CanSeeThread(thread))
            {
                return NotFound(new { message = "Thread not found" });
            }

            var poll = _pollRepository.GetByThreadId(id);
            if (poll == null)
            {
                return NotFound(new { message = "This thread has no poll" });
            }

            if (!poll.Options.Any(o => o.Id == request.PollOptionId))
            {
                return BadRequest(new { message = "Invalid poll option" });
            }

            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            if (_pollRepository.HasVoted(poll.Id, userId))
            {
                return BadRequest(new { message = "You have already voted on this poll" });
            }

            _pollRepository.AddVote(poll.Id, request.PollOptionId, userId);

            var updatedPoll = _pollRepository.GetByThreadId(id)!;
            var voteCounts = _pollRepository.GetVoteCounts(updatedPoll.Id);
            var votedOptionId = _pollRepository.GetVotedOptionId(updatedPoll.Id, userId);

            return Ok(new PollResponse
            {
                Id = updatedPoll.Id,
                ThreadId = updatedPoll.ThreadId,
                Question = updatedPoll.Question,
                Options = updatedPoll.Options
                    .Select(o => new PollOptionResponse
                    {
                        Id = o.Id,
                        Text = o.Text,
                        VoteCount = voteCounts.TryGetValue(o.Id, out var count) ? count : 0
                    })
                    .ToList(),
                TotalVotes = voteCounts.Values.Sum(),
                HasVoted = votedOptionId != null,
                VotedOptionId = votedOptionId
            });
        }

        [HttpPut("{id}/subscription")]
        [Authorize]
        public ActionResult SubscribeToThread(int id, SubscribeToThreadRequest request)
        {
            var thread = _threadRepository.GetById(id);
            if (!_visibility.CanSeeThread(thread))
            {
                return NotFound(new { message = "Thread not found" });
            }

            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var existing = _subscriptionRepository.GetByThreadAndUser(id, userId);
            if (existing == null)
            {
                _subscriptionRepository.Add(new MvcBB.Shared.Models.ThreadSubscription.ThreadSubscriptionModel
                {
                    ThreadId = id,
                    UserId = userId,
                    NotifyViaMessage = request.NotifyViaMessage,
                    NotifyViaEmail = request.NotifyViaEmail
                });
            }
            else
            {
                existing.NotifyViaMessage = request.NotifyViaMessage;
                existing.NotifyViaEmail = request.NotifyViaEmail;
                _subscriptionRepository.Update(existing);
            }

            return Ok(new { message = "Subscribed to thread" });
        }

        [HttpDelete("{id}/subscription")]
        [Authorize]
        public ActionResult UnsubscribeFromThread(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            _subscriptionRepository.Remove(id, userId);
            return Ok(new { message = "Unsubscribed from thread" });
        }

        [HttpPut("{id}")]
        [Authorize]
        public ActionResult<ThreadResponse> UpdateThread(int id, UpdateForumThreadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var thread = _threadRepository.GetById(id);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            if (thread.CreatedByUserId != userId &&
                !User.IsInRole("Administrator") &&
                !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            var oldTitle = thread.Title;
            var wasSticky = thread.IsSticky;
            var wasLocked = thread.IsLocked;
            thread.Title = request.Title;

            // Only staff can pin or lock a thread. A thread's author can still
            // rename it, but whatever they send for these two flags is ignored,
            // so they can't undo a moderator's lock or pin their own thread.
            if (User.IsInRole("Administrator") || User.IsInRole("Moderator"))
            {
                thread.IsSticky = request.IsSticky;
                thread.IsLocked = request.IsLocked;
            }

            thread.UpdatedAt = DateTime.UtcNow;
            _threadRepository.Update(thread);

            // The moderation log only takes staff actions (see ModerationLogger),
            // so an author renaming their own thread isn't logged.
            var authorId = MvcBB.API.Services.ModerationLogger.ParseUserId(thread.CreatedByUserId);
            if (thread.CreatedByUserId != userId && oldTitle != thread.Title)
            {
                _moderationLog.Log(MvcBB.Shared.Models.Moderation.ModerationActions.RenameThread, "Thread", thread.Id, authorId,
                    $"Renamed \"{oldTitle}\" to \"{thread.Title}\"");
            }
            if (wasLocked != thread.IsLocked)
            {
                _moderationLog.Log(thread.IsLocked ? MvcBB.Shared.Models.Moderation.ModerationActions.LockThread : MvcBB.Shared.Models.Moderation.ModerationActions.UnlockThread,
                    "Thread", thread.Id, authorId, $"\"{thread.Title}\"");
            }
            if (wasSticky != thread.IsSticky)
            {
                _moderationLog.Log(thread.IsSticky ? MvcBB.Shared.Models.Moderation.ModerationActions.PinThread : MvcBB.Shared.Models.Moderation.ModerationActions.UnpinThread,
                    "Thread", thread.Id, authorId, $"\"{thread.Title}\"");
            }

            return Ok(MapToThreadResponses(new[] { thread }).Single());
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult DeleteThread(int id)
        {
            var thread = _threadRepository.GetById(id);
            if (thread == null)
            {
                return NotFound(new { message = "Thread not found" });
            }

            // The thread (and its posts) go first: only once that has worked
            // does the board lose them from its counts.
            _threadRepository.Remove(thread);

            var board = _boardRepository.GetById(thread.BoardId);
            if (board != null)
            {
                board.ThreadCount = Math.Max(0, board.ThreadCount - 1);
                board.PostCount = Math.Max(0, board.PostCount - thread.PostCount);
                _boardRepository.Update(board);
            }

            _moderationLog.Log(MvcBB.Shared.Models.Moderation.ModerationActions.DeleteThread, "Thread", thread.Id,
                MvcBB.API.Services.ModerationLogger.ParseUserId(thread.CreatedByUserId),
                $"Deleted \"{thread.Title}\" from {(string.IsNullOrEmpty(thread.BoardName) ? "its board" : thread.BoardName)} ({thread.PostCount} {(thread.PostCount == 1 ? "post" : "posts")})");

            return Ok(new { message = "Thread deleted successfully" });
        }

        /// <summary>
        /// Maps a page's worth of threads to responses with a fixed, small
        /// number of batched queries total (board/username are already on
        /// ForumThread via vw_ThreadDetails; TotalLikes/ParticipantCount are
        /// now stored columns on Threads, maintained incrementally, rather
        /// than a like-totals/distinct-participant aggregate recomputed here
        /// on every request) - instead of poll and subscription each costing
        /// a separate round trip per thread.
        /// </summary>
        private List<ThreadResponse> MapToThreadResponses(IReadOnlyList<ForumThread> threads)
        {
            if (threads.Count == 0)
            {
                return new List<ThreadResponse>();
            }

            var threadIds = threads.Select(t => t.Id).ToList();
            var currentUserId = CurrentUserId;

            var pollsByThreadId = _pollRepository.GetByThreadIds(threadIds);
            var pollIds = pollsByThreadId.Values.Select(p => p.Id).ToList();
            var voteCountsByPollId = _pollRepository.GetVoteCountsByPollIds(pollIds);
            var votedOptionsByPollId = currentUserId != null
                ? _pollRepository.GetVotedOptionIdsByPollIds(pollIds, currentUserId)
                : new Dictionary<int, int>();

            var subscriptionsByThreadId = currentUserId != null
                ? _subscriptionRepository.GetByThreadIdsAndUser(threadIds, currentUserId)
                : new Dictionary<int, MvcBB.Shared.Models.ThreadSubscription.ThreadSubscriptionModel>();

            return threads.Select(t =>
            {
                PollResponse? pollResponse = null;
                if (pollsByThreadId.TryGetValue(t.Id, out var poll))
                {
                    var voteCounts = voteCountsByPollId.TryGetValue(poll.Id, out var counts) ? counts : new Dictionary<int, int>();
                    var votedOptionId = votedOptionsByPollId.TryGetValue(poll.Id, out var voted) ? (int?)voted : null;
                    pollResponse = new PollResponse
                    {
                        Id = poll.Id,
                        ThreadId = poll.ThreadId,
                        Question = poll.Question,
                        Options = poll.Options
                            .Select(o => new PollOptionResponse
                            {
                                Id = o.Id,
                                Text = o.Text,
                                VoteCount = voteCounts.TryGetValue(o.Id, out var count) ? count : 0
                            })
                            .ToList(),
                        TotalVotes = voteCounts.Values.Sum(),
                        HasVoted = votedOptionId != null,
                        VotedOptionId = votedOptionId
                    };
                }

                subscriptionsByThreadId.TryGetValue(t.Id, out var subscription);

                return new ThreadResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    BoardId = t.BoardId,
                    BoardName = t.BoardName,
                    CreatedByUserId = t.CreatedByUserId,
                    CreatedByUsername = t.CreatedByUsername,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    IsSticky = t.IsSticky,
                    IsLocked = t.IsLocked,
                    IsImportedData = t.IsImportedData,
                    ViewCount = t.ViewCount,
                    // PostCount includes the thread's opening post, so replies
                    // are every post after the first.
                    ReplyCount = Math.Max(0, t.PostCount - 1),
                    LastPostAt = t.LastPostAt,
                    LastPostByUserId = t.LastPostByUserId,
                    LastPostByUsername = t.LastPostByUsername,
                    TotalLikes = t.TotalLikes,
                    ParticipantCount = t.ParticipantCount,
                    Poll = pollResponse,
                    IsSubscribed = subscription != null,
                    NotifyViaMessage = subscription?.NotifyViaMessage ?? false,
                    NotifyViaEmail = subscription?.NotifyViaEmail ?? false
                };
            }).ToList();
        }

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
