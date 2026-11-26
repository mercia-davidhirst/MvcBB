using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.API.Services;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Moderation;
using MvcBB.Shared.Models.Poll;

namespace MvcBB.API.Controllers
{
    /// <summary>
    /// The moderation queue (posts held for approval), the moderation log and
    /// the settings for holding new members' posts. The queue and log are for
    /// Administrators and Moderators; changing the settings is for
    /// Administrators.
    /// </summary>
    [ApiController]
    [Route("api/moderation")]
    [Authorize(Roles = "Administrator,Moderator")]
    public class ModerationController : ControllerBase
    {
        private readonly IModerationQueueRepository _queue;
        private readonly IModerationLogRepository _log;
        private readonly IModerationSettingsRepository _settings;
        private readonly IBoardRepository _boards;
        private readonly IForumThreadRepository _threads;
        private readonly IPostRepository _posts;
        private readonly IForumPostingService _posting;
        private readonly INotificationService _notifications;
        private readonly IModerationLogger _moderationLog;

        public ModerationController(
            IModerationQueueRepository queue,
            IModerationLogRepository log,
            IModerationSettingsRepository settings,
            IBoardRepository boards,
            IForumThreadRepository threads,
            IPostRepository posts,
            IForumPostingService posting,
            INotificationService notifications,
            IModerationLogger moderationLog)
        {
            _queue = queue;
            _log = log;
            _settings = settings;
            _boards = boards;
            _threads = threads;
            _posts = posts;
            _posting = posting;
            _notifications = notifications;
            _moderationLog = moderationLog;
        }

        [HttpGet("queue")]
        public ActionResult<IEnumerable<ModerationQueueItem>> GetQueue() => Ok(_queue.GetAll());

        [HttpGet("queue/count")]
        public ActionResult GetQueueCount() => Ok(new { count = _queue.Count() });

        [HttpGet("queue/{id:int}")]
        public ActionResult<ModerationQueueItem> GetQueueItem(int id)
        {
            var item = _queue.GetById(id);
            return item == null ? NotFound(new { message = "That post is no longer waiting for approval." }) : Ok(item);
        }

        /// <summary>
        /// Publishes a held post as if its author had just posted it (word
        /// filter, counts, watchers and notifications included), then removes
        /// it from the queue and tells the author.
        /// </summary>
        [HttpPost("queue/{id:int}/approve")]
        public async Task<ActionResult<ApproveQueuedPostResponse>> Approve(int id)
        {
            var item = _queue.GetById(id);
            if (item == null)
            {
                return NotFound(new { message = "That post is no longer waiting for approval." });
            }

            ApproveQueuedPostResponse result;
            string where;
            if (item.Kind == QueuedPostKind.NewThread)
            {
                var board = _boards.GetById(item.BoardId);
                if (board == null)
                {
                    return BadRequest(new { message = "The board this discussion was posted in no longer exists. Turn it down instead." });
                }

                CreatePollRequest? poll = null;
                if (!string.IsNullOrEmpty(item.PollJson))
                {
                    try
                    {
                        poll = JsonSerializer.Deserialize<CreatePollRequest>(item.PollJson);
                    }
                    catch (JsonException)
                    {
                        // A damaged poll shouldn't block the discussion itself.
                    }
                }

                var (thread, firstPost) = _posting.CreateThread(item.UserId, board, item.Title ?? "(untitled)", item.Content, poll);
                result = new ApproveQueuedPostResponse { ThreadId = thread.Id, PostId = firstPost.Id };
                where = $"\"{thread.Title}\"";
            }
            else
            {
                var thread = item.ThreadId.HasValue ? _threads.GetById(item.ThreadId.Value) : null;
                if (thread == null)
                {
                    return BadRequest(new { message = "The discussion this reply belongs to no longer exists. Turn it down instead." });
                }

                // The quoted post may have gone in the meantime; keep the reply, drop the quote.
                var quotedPostId = item.QuotedPostId.HasValue && _posts.GetById(item.QuotedPostId.Value) is { } quoted && quoted.ThreadId == thread.Id
                    ? item.QuotedPostId
                    : null;

                var post = await _posting.CreateReplyAsync(item.UserId, thread, item.Content, quotedPostId, item.ImageUrl);
                result = new ApproveQueuedPostResponse { ThreadId = thread.Id, PostId = post.Id };
                where = $"\"{thread.Title}\"";
            }

            _queue.Remove(item.Id);

            _moderationLog.Log(ModerationActions.ApprovePost, "Post", result.PostId, item.UserId,
                item.Kind == QueuedPostKind.NewThread ? $"Approved new discussion {where}" : $"Approved a reply in {where}");

            if (CurrentUserId is { } moderatorId)
            {
                _notifications.NotifyModeration(item.UserId, moderatorId,
                    item.Kind == QueuedPostKind.NewThread
                        ? $"Your discussion {where} has been approved and is now live."
                        : $"Your reply in {where} has been approved and is now live.",
                    result.ThreadId, result.PostId);
            }

            return Ok(result);
        }

        /// <summary>
        /// Turns down a held post: it's deleted (never published) and its
        /// author is told, with the reason if one was given. Returns the
        /// removed item, so the App can delete any image that came with it.
        /// </summary>
        [HttpPost("queue/{id:int}/reject")]
        public ActionResult<ModerationQueueItem> Reject(int id, [FromBody] RejectQueuedPostRequest? request)
        {
            var item = _queue.GetById(id);
            if (item == null)
            {
                return NotFound(new { message = "That post is no longer waiting for approval." });
            }

            _queue.Remove(item.Id);

            var reason = string.IsNullOrWhiteSpace(request?.Reason) ? null : request!.Reason!.Trim();
            var what = item.Kind == QueuedPostKind.NewThread
                ? $"new discussion \"{item.Title}\" in {item.BoardName ?? "a board"}"
                : $"a reply in \"{item.ThreadTitle ?? "a discussion"}\"";

            _moderationLog.Log(ModerationActions.RejectPost, "Queue", item.Id, item.UserId,
                $"Turned down {what}: {_notifications.Excerpt(item.Content, 200)}", reason);

            if (CurrentUserId is { } moderatorId)
            {
                var text = item.Kind == QueuedPostKind.NewThread
                    ? $"Your discussion \"{item.Title}\" wasn't approved."
                    : $"Your reply in \"{item.ThreadTitle}\" wasn't approved.";
                if (reason != null)
                {
                    text += " Reason: " + reason;
                }
                _notifications.NotifyModeration(item.UserId, moderatorId, text,
                    item.Kind == QueuedPostKind.Reply ? item.ThreadId : null, null);
            }

            return Ok(item);
        }

        [HttpGet("log")]
        public ActionResult<ModerationLogPage> GetLog([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var (items, total) = _log.GetPage((page - 1) * pageSize, pageSize);
            return Ok(new ModerationLogPage
            {
                Entries = items.ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("settings")]
        public ActionResult<ModerationSettings> GetSettings() => Ok(_settings.Get());

        [HttpPut("settings")]
        [Authorize(Roles = "Administrator")]
        public ActionResult PutSettings([FromBody] ModerationSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var before = _settings.Get();
            settings.NewMemberPostThreshold = Math.Clamp(settings.NewMemberPostThreshold, 1, 100);
            _settings.Update(settings);

            if (before.HoldNewMemberPosts != settings.HoldNewMemberPosts || before.NewMemberPostThreshold != settings.NewMemberPostThreshold)
            {
                _moderationLog.Log(ModerationActions.ChangeSettings, "Settings", null, null,
                    settings.HoldNewMemberPosts
                        ? $"New members' first {settings.NewMemberPostThreshold} {(settings.NewMemberPostThreshold == 1 ? "post is" : "posts are")} now held for approval"
                        : "New members' posts are no longer held for approval");
            }

            return NoContent();
        }

        private int? CurrentUserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
