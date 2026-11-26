using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Report;
using MvcBB.Shared.Interfaces;
using System.Security.Claims;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportRepository _reportRepository;
        private readonly IForumThreadRepository _threadRepository;
        private readonly IPostRepository _postRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly MvcBB.API.Services.IModerationLogger? _moderationLog;

        public ReportsController(
            IReportRepository reportRepository,
            IForumThreadRepository threadRepository,
            IPostRepository postRepository,
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            MvcBB.API.Services.IModerationLogger? moderationLog = null)
        {
            _moderationLog = moderationLog;
            _reportRepository = reportRepository;
            _threadRepository = threadRepository;
            _postRepository = postRepository;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult<IEnumerable<ReportResponse>> GetReports([FromQuery] ReportStatus? status = null)
        {
            var reports = _reportRepository.GetAll().AsEnumerable();
            if (status.HasValue)
            {
                reports = reports.Where(r => r.Status == status.Value);
            }

            return Ok(MapToReportResponses(reports.ToList()));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult<ReportResponse> GetReport(int id)
        {
            var report = _reportRepository.GetById(id);
            if (report == null)
            {
                return NotFound(new { message = "Report not found" });
            }

            // A reported private message's body is shown to moderators and
            // administrators on the report details page only (this endpoint is
            // already limited to those roles; checked again so the rule holds
            // even if the attribute ever changes).
            var includeMessageContent = User.IsInRole("Administrator") || User.IsInRole("Moderator");
            return Ok(MapToReportResponses(new[] { report }, includeMessageContent).Single());
        }

        [HttpPost]
        [Authorize]
        public ActionResult<ReportResponse> CreateReport(CreateReportRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            if (!Enum.IsDefined(request.Type))
            {
                return BadRequest(new { message = "Unknown report type" });
            }

            // Private messages can only be reported by their recipient (the
            // sender can just delete their own message, and nobody else can
            // see it). A missing message and someone else's message get the
            // same 404 "Message not found" the message endpoints use for a
            // missing id, so the response can't be used to probe which
            // message ids exist.
            if (request.Type == ReportType.Message)
            {
                var message = _messageRepository.GetById(request.ContentId);
                if (message == null || !string.Equals(message.RecipientUserId, userId, StringComparison.Ordinal))
                {
                    return NotFound(new { message = "Message not found" });
                }
            }

            // A site issue isn't about any content, so it never carries a
            // content id; only it may carry a page address, and only a
            // site-relative one (see ReportPageUrl).
            var isSiteIssue = request.Type == ReportType.SiteIssue;
            var report = new Report
            {
                Reason = request.Reason,
                Type = request.Type,
                ContentId = isSiteIssue ? 0 : request.ContentId,
                PageUrl = isSiteIssue ? ReportPageUrl.Normalize(request.PageUrl) : null,
                ReporterUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending
            };

            report = _reportRepository.Add(report);

            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, MapToReportResponse(report));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult<ReportResponse> UpdateReport(int id, UpdateReportRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var report = _reportRepository.GetById(id);
            if (report == null)
            {
                return NotFound(new { message = "Report not found" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var oldStatus = report.Status;
            report.Status = request.Status;
            report.ModeratorNotes = request.ModeratorNotes ?? string.Empty;
            report.ResolvedByUserId = userId;
            report.ResolvedAt = DateTime.UtcNow;
            _reportRepository.Update(report);

            _moderationLog?.Log(MvcBB.Shared.Models.Moderation.ModerationActions.UpdateReport, "Report", report.Id,
                MvcBB.API.Services.ModerationLogger.ParseUserId(report.ReporterUserId),
                oldStatus == report.Status ? $"Updated notes on report #{report.Id} ({report.Status})" : $"Report #{report.Id}: {oldStatus} to {report.Status}",
                string.IsNullOrWhiteSpace(report.ModeratorNotes) ? null : report.ModeratorNotes);

            return Ok(MapToReportResponse(report));
        }

        /// <summary>
        /// Reports still waiting on a moderator (Pending or Investigating).
        /// Polled by the App's navbar alert, so it's a single count query
        /// rather than the full stats object.
        /// </summary>
        [HttpGet("open-count")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult GetOpenCount()
        {
            return Ok(new { openCount = _reportRepository.CountOpen() });
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult GetReportStats()
        {
            var stats = new
            {
                TotalReports = _reportRepository.Count(),
                PendingReports = _reportRepository.CountByStatus(ReportStatus.Pending),
                InvestigatingReports = _reportRepository.CountByStatus(ReportStatus.Investigating),
                ResolvedReports = _reportRepository.CountByStatus(ReportStatus.Resolved),
                DismissedReports = _reportRepository.CountByStatus(ReportStatus.Dismissed),
                ReportsByType = new
                {
                    ThreadReports = _reportRepository.CountByType(ReportType.Thread),
                    PostReports = _reportRepository.CountByType(ReportType.Post),
                    MessageReports = _reportRepository.CountByType(ReportType.Message),
                    UserReports = _reportRepository.CountByType(ReportType.User),
                    SiteIssueReports = _reportRepository.CountByType(ReportType.SiteIssue)
                }
            };

            return Ok(stats);
        }

        private ReportResponse MapToReportResponse(Report r) => MapToReportResponses(new[] { r }).Single();

        /// <summary>
        /// Maps a page's worth of reports. The referenced content (thread/
        /// post/message/user) still costs one lookup per report by type -
        /// reports are low-volume and admin/moderator-only, unlike threads/
        /// posts/users elsewhere in this API, so that isn't the same class of
        /// problem - but every reporter/resolver/content-author username
        /// (up to three per report) is resolved via a single batched call for
        /// the whole page instead of up to three GetUsername calls per report.
        /// </summary>
        /// <param name="includeMessageContent">
        /// Fill MessageContent/recipient for Message reports. Only ever true for
        /// a moderator/administrator viewing one report's details.
        /// </param>
        private List<ReportResponse> MapToReportResponses(IReadOnlyList<Report> reports, bool includeMessageContent = false)
        {
            if (reports.Count == 0)
            {
                return new List<ReportResponse>();
            }

            var responses = new List<ReportResponse>(reports.Count);
            var userIdsNeeded = new HashSet<int>();

            void Track(string? userId)
            {
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var id))
                {
                    userIdsNeeded.Add(id);
                }
            }

            foreach (var r in reports)
            {
                Track(r.ReporterUserId);
                Track(r.ResolvedByUserId);

                var response = new ReportResponse
                {
                    Id = r.Id,
                    Reason = r.Reason,
                    ReporterUserId = r.ReporterUserId,
                    CreatedAt = r.CreatedAt,
                    Type = r.Type,
                    ContentId = r.ContentId,
                    Status = r.Status,
                    ModeratorNotes = r.ModeratorNotes,
                    ResolvedAt = r.ResolvedAt,
                    ResolvedByUserId = r.ResolvedByUserId,
                    PageUrl = r.PageUrl
                };

                switch (r.Type)
                {
                    case ReportType.Thread:
                        var thread = _threadRepository.GetById(r.ContentId);
                        if (thread != null)
                        {
                            response.ContentTitle = thread.Title;
                            response.ContentThreadId = thread.Id;
                            response.ContentAuthorId = thread.CreatedByUserId;
                            response.ContentCreatedAt = thread.CreatedAt;
                            Track(thread.CreatedByUserId);
                        }
                        break;

                    case ReportType.Post:
                        var post = _postRepository.GetById(r.ContentId);
                        if (post != null)
                        {
                            var postThread = _threadRepository.GetById(post.ThreadId);
                            response.ContentTitle = postThread?.Title ?? $"Post #{post.Id}";
                            response.ContentThreadId = post.ThreadId;
                            response.ContentAuthorId = post.CreatedByUserId;
                            response.ContentCreatedAt = post.CreatedAt;
                            Track(post.CreatedByUserId);
                        }
                        break;

                    case ReportType.Message:
                        var message = _messageRepository.GetById(r.ContentId);
                        if (message != null)
                        {
                            response.ContentTitle = message.Subject;
                            response.ContentAuthorId = message.SenderUserId;
                            response.ContentCreatedAt = message.CreatedAt;
                            Track(message.SenderUserId);
                            if (includeMessageContent)
                            {
                                response.MessageContent = message.Content;
                                response.MessageRecipientUserId = message.RecipientUserId;
                                Track(message.RecipientUserId);
                            }
                        }
                        break;

                    case ReportType.User:
                        var user = _userRepository.GetById(r.ContentId);
                        if (user != null)
                        {
                            response.ContentTitle = user.Username;
                            response.ContentAuthorId = r.ContentId.ToString();
                            response.ContentCreatedAt = user.CreatedAt;
                            Track(response.ContentAuthorId);
                        }
                        break;

                    case ReportType.SiteIssue:
                        // Nothing to look up: the "content" is the page the
                        // reporter was on. ContentCreatedAt stays unset and the
                        // views don't show it for this type.
                        response.ContentTitle = string.IsNullOrEmpty(r.PageUrl) ? "Site issue" : r.PageUrl;
                        break;
                }

                responses.Add(response);
            }

            var usernames = _userRepository.GetUsernamesByIds(userIdsNeeded);
            string Resolve(string? userId) =>
                !string.IsNullOrEmpty(userId) && int.TryParse(userId, out var id) && usernames.TryGetValue(id, out var name)
                    ? name
                    : string.Empty;

            foreach (var response in responses)
            {
                response.ReporterUsername = Resolve(response.ReporterUserId);
                response.ResolvedByUsername = Resolve(response.ResolvedByUserId);
                response.ContentAuthorUsername = Resolve(response.ContentAuthorId);
                response.MessageRecipientUsername = Resolve(response.MessageRecipientUserId);
            }

            return responses;
        }
    }
}
