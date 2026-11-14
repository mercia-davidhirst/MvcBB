using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Poll;
using MvcBB.Shared.Models.Post;
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
        private readonly IPostLikeRepository _postLikeRepository;
        private readonly IPollRepository _pollRepository;
        private readonly IWordFilterRepository _wordFilterRepository;
        private readonly IThreadSubscriptionRepository _subscriptionRepository;
        private readonly MvcBB.API.Services.IDisplaySettingsService _displaySettingsService;

        public ThreadsController(
            IForumThreadRepository threadRepository,
            IBoardRepository boardRepository,
            IPostRepository postRepository,
            IUserRepository userRepository,
            IPostLikeRepository postLikeRepository,
            IPollRepository pollRepository,
            IWordFilterRepository wordFilterRepository,
            IThreadSubscriptionRepository subscriptionRepository,
            MvcBB.API.Services.IDisplaySettingsService displaySettingsService)
        {
            _threadRepository = threadRepository;
            _boardRepository = boardRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _postLikeRepository = postLikeRepository;
            _pollRepository = pollRepository;
            _wordFilterRepository = wordFilterRepository;
            _subscriptionRepository = subscriptionRepository;
            _displaySettingsService = displaySettingsService;
        }

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

            return Ok(new ThreadListResponse
            {
                Threads = MapToThreadResponses(pageOfThreads),
                TotalThreads = totalThreads,
                Page = page,
                PageSize = effectivePageSize,
                TotalPages = totalPages
            });
        }

        [HttpGet("board/{boardId}")]
        public ActionResult<ThreadListResponse> GetBoardThreads(int boardId, [FromQuery] int page = 1, [FromQuery] int? pageSize = null)
        {
            var board = _boardRepository.GetById(boardId);
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            if (board == null || !board.IsVisibleTo(isAuthenticated))
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

        [HttpGet("{id}")]
        public ActionResult<ThreadResponse> GetThread(int id)
        {
            var thread = _threadRepository.GetById(id);
            if (thread == null || !IsThreadVisible(thread, User.Identity?.IsAuthenticated == true))
            {
                return NotFound(new { message = "Thread not found" });
            }

            thread.ViewCount++;
            _threadRepository.Update(thread);

            return Ok(MapToThreadResponses(new[] { thread }).Single());
        }

        private static bool IsThreadVisible(ForumThread thread, bool isAuthenticated) =>
            isAuthenticated || !thread.BoardIsPrivate;

        [HttpPost]
        [Authorize]
        public ActionResult<ThreadResponse> CreateThread(CreateForumThreadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = _boardRepository.GetById(request.BoardId);
            if (board == null)
            {
                return NotFound(new { message = "Board not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var wordFilters = _wordFilterRepository.GetAll();
            var now = DateTime.UtcNow;
            var thread = new ForumThread
            {
                Title = WordFilterApplier.Apply(request.Title, wordFilters),
                BoardId = request.BoardId,
                CreatedByUserId = userId,
                CreatedAt = now,
                LastPostAt = now,
                LastPostByUserId = userId,
                PostCount = 1
            };

            thread = _threadRepository.Add(thread);

            // The thread's own Content is its first post - without this the thread
            // would have zero posts and nothing for GetThreadPosts to return.
            _postRepository.Add(new Post
            {
                Content = WordFilterApplier.Apply(request.Content, wordFilters),
                ThreadId = thread.Id,
                CreatedByUserId = userId,
                CreatedAt = now
            });

            board.ThreadCount++;
            board.PostCount++;
            board.LastPostAt = now;
            board.LastPostByUserId = userId;
            _boardRepository.Update(board);

            if (request.Poll != null)
            {
                _pollRepository.Add(new Poll
                {
                    ThreadId = thread.Id,
                    Question = request.Poll.Question,
                    CreatedAt = now,
                    Options = request.Poll.Options
                        .Select((text, index) => new PollOption { Text = text, SortOrder = index })
                        .ToList()
                });
            }

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
            if (thread == null)
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
            if (thread == null)
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

            thread.Title = request.Title;
            thread.IsSticky = request.IsSticky;
            thread.IsLocked = request.IsLocked;
            thread.UpdatedAt = DateTime.UtcNow;
            _threadRepository.Update(thread);

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

            var board = _boardRepository.GetById(thread.BoardId);
            if (board != null)
            {
                board.ThreadCount--;
                _boardRepository.Update(board);
            }

            _threadRepository.Remove(thread);
            return Ok(new { message = "Thread deleted successfully" });
        }

        /// <summary>
        /// Maps a page's worth of threads to responses with a fixed, small
        /// number of batched queries total (board/username are already on
        /// ForumThread via vw_ThreadDetails) - instead of participant count,
        /// like totals, poll, and subscription each costing a separate
        /// round trip per thread.
        /// </summary>
        private List<ThreadResponse> MapToThreadResponses(IReadOnlyList<ForumThread> threads)
        {
            if (threads.Count == 0)
            {
                return new List<ThreadResponse>();
            }

            var threadIds = threads.Select(t => t.Id).ToList();
            var currentUserId = CurrentUserId;

            var summaries = _postRepository.GetThreadSummaries(threadIds);
            var allPostIds = summaries.Values.SelectMany(s => s.PostIds).ToList();
            var likeCounts = _postLikeRepository.GetLikeCounts(allPostIds);

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
                summaries.TryGetValue(t.Id, out var summary);
                var postIds = summary?.PostIds ?? Array.Empty<int>();

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
                    ViewCount = t.ViewCount,
                    ReplyCount = t.PostCount,
                    LastPostAt = t.LastPostAt,
                    LastPostByUserId = t.LastPostByUserId,
                    LastPostByUsername = t.LastPostByUsername,
                    TotalLikes = postIds.Sum(id => likeCounts.TryGetValue(id, out var c) ? c : 0),
                    ParticipantCount = summary?.ParticipantCount ?? 0,
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
