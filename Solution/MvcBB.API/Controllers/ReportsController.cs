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

        public ReportsController(
            IReportRepository reportRepository,
            IForumThreadRepository threadRepository,
            IPostRepository postRepository,
            IMessageRepository messageRepository,
            IUserRepository userRepository)
        {
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

            return Ok(reports.Select(MapToReportResponse));
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

            return Ok(MapToReportResponse(report));
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

            var report = new Report
            {
                Reason = request.Reason,
                Type = request.Type,
                ContentId = request.ContentId,
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

            report.Status = request.Status;
            report.ModeratorNotes = request.ModeratorNotes ?? string.Empty;
            report.ResolvedByUserId = userId;
            report.ResolvedAt = DateTime.UtcNow;
            _reportRepository.Update(report);

            return Ok(MapToReportResponse(report));
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
                    MessageReports = _reportRepository.CountByType(ReportType.Message)
                }
            };

            return Ok(stats);
        }

        private ReportResponse MapToReportResponse(Report r)
        {
            var response = new ReportResponse
            {
                Id = r.Id,
                Reason = r.Reason,
                ReporterUserId = r.ReporterUserId,
                ReporterUsername = GetUsername(r.ReporterUserId),
                CreatedAt = r.CreatedAt,
                Type = r.Type,
                ContentId = r.ContentId,
                Status = r.Status,
                ModeratorNotes = r.ModeratorNotes,
                ResolvedAt = r.ResolvedAt,
                ResolvedByUserId = r.ResolvedByUserId,
                ResolvedByUsername = GetUsername(r.ResolvedByUserId)
            };

            switch (r.Type)
            {
                case ReportType.Thread:
                    var thread = _threadRepository.GetById(r.ContentId);
                    if (thread != null)
                    {
                        response.ContentTitle = thread.Title;
                        response.ContentAuthorId = thread.CreatedByUserId;
                        response.ContentAuthorUsername = GetUsername(thread.CreatedByUserId);
                        response.ContentCreatedAt = thread.CreatedAt;
                    }
                    break;

                case ReportType.Post:
                    var post = _postRepository.GetById(r.ContentId);
                    if (post != null)
                    {
                        var postThread = _threadRepository.GetById(post.ThreadId);
                        response.ContentTitle = postThread?.Title ?? $"Post #{post.Id}";
                        response.ContentAuthorId = post.CreatedByUserId;
                        response.ContentAuthorUsername = GetUsername(post.CreatedByUserId);
                        response.ContentCreatedAt = post.CreatedAt;
                    }
                    break;

                case ReportType.Message:
                    var message = _messageRepository.GetById(r.ContentId);
                    if (message != null)
                    {
                        response.ContentTitle = message.Subject;
                        response.ContentAuthorId = message.SenderUserId;
                        response.ContentAuthorUsername = GetUsername(message.SenderUserId);
                        response.ContentCreatedAt = message.CreatedAt;
                    }
                    break;

                case ReportType.User:
                    var user = _userRepository.GetById(r.ContentId);
                    if (user != null)
                    {
                        response.ContentTitle = user.Username;
                        response.ContentAuthorId = r.ContentId.ToString();
                        response.ContentAuthorUsername = user.Username;
                        response.ContentCreatedAt = user.CreatedAt;
                    }
                    break;
            }

            return response;
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
