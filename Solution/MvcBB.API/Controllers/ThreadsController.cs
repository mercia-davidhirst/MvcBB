using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Poll;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Interfaces;
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
        private readonly MvcBB.API.Services.IDisplaySettingsService _displaySettingsService;

        public ThreadsController(
            IForumThreadRepository threadRepository,
            IBoardRepository boardRepository,
            IPostRepository postRepository,
            IUserRepository userRepository,
            IPostLikeRepository postLikeRepository,
            IPollRepository pollRepository,
            MvcBB.API.Services.IDisplaySettingsService displaySettingsService)
        {
            _threadRepository = threadRepository;
            _boardRepository = boardRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _postLikeRepository = postLikeRepository;
            _pollRepository = pollRepository;
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
            var ordered = _threadRepository.GetAll()
                .Where(t => IsThreadVisible(t, isAuthenticated))
                .OrderByDescending(t => t.IsSticky)
                .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt)
                .ToList();

            var totalThreads = ordered.Count;
            var totalPages = totalThreads == 0 ? 1 : (int)Math.Ceiling(totalThreads / (double)effectivePageSize);
            var skip = (page - 1) * effectivePageSize;
            var pageOfThreads = ordered.Skip(skip).Take(effectivePageSize).ToList();

            var threadResponses = pageOfThreads.Select(t => MapToThreadResponse(t)).ToList();

            return Ok(new ThreadListResponse
            {
                Threads = threadResponses,
                TotalThreads = totalThreads,
                Page = page,
                PageSize = effectivePageSize,
                TotalPages = totalPages
            });
        }

        [HttpGet("board/{boardId}")]
        public ActionResult<IEnumerable<ThreadResponse>> GetBoardThreads(int boardId)
        {
            var board = _boardRepository.GetById(boardId);
            if (board == null || !board.IsVisibleTo(User.Identity?.IsAuthenticated == true))
            {
                return NotFound(new { message = "Board not found" });
            }

            var threads = _threadRepository.GetByBoardId(boardId);
            return Ok(threads.Select(MapToThreadResponse));
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

            return Ok(MapToThreadResponse(thread));
        }

        private bool IsThreadVisible(ForumThread thread, bool isAuthenticated)
        {
            var board = _boardRepository.GetById(thread.BoardId);
            return board == null || board.IsVisibleTo(isAuthenticated);
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
            if (board == null)
            {
                return NotFound(new { message = "Board not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var now = DateTime.UtcNow;
            var thread = new ForumThread
            {
                Title = request.Title,
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
                Content = request.Content,
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

            return CreatedAtAction(nameof(GetThread), new { id = thread.Id }, MapToThreadResponse(thread));
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

            return Ok(MapToPollResponse(_pollRepository.GetByThreadId(id)));
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

            return Ok(MapToThreadResponse(thread));
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

        private ThreadResponse MapToThreadResponse(ForumThread t)
        {
            var board = _boardRepository.GetById(t.BoardId);
            var posts = _postRepository.GetByThreadId(t.Id);
            var postIds = posts.Select(p => p.Id).ToList();
            var likeCounts = _postLikeRepository.GetLikeCounts(postIds);

            return new ThreadResponse
            {
                Id = t.Id,
                Title = t.Title,
                BoardId = t.BoardId,
                BoardName = board?.Name ?? string.Empty,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByUsername = GetUsername(t.CreatedByUserId),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                IsSticky = t.IsSticky,
                IsLocked = t.IsLocked,
                ViewCount = t.ViewCount,
                ReplyCount = t.PostCount,
                LastPostAt = t.LastPostAt,
                LastPostByUserId = t.LastPostByUserId,
                LastPostByUsername = GetUsername(t.LastPostByUserId),
                TotalLikes = likeCounts.Values.Sum(),
                ParticipantCount = posts.Select(p => p.CreatedByUserId).Distinct().Count(),
                Poll = MapToPollResponse(_pollRepository.GetByThreadId(t.Id))
            };
        }

        private PollResponse? MapToPollResponse(Poll? poll)
        {
            if (poll == null)
            {
                return null;
            }

            var voteCounts = _pollRepository.GetVoteCounts(poll.Id);
            var votedOptionId = CurrentUserId != null ? _pollRepository.GetVotedOptionId(poll.Id, CurrentUserId) : null;

            return new PollResponse
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

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
