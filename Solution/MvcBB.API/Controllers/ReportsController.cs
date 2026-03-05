using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Report;
using System.Security.Claims;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private static readonly List<Report> _reports = new();

        [HttpGet]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult<IEnumerable<Report>> GetReports()
        {
            return Ok(_reports.OrderByDescending(r => r.CreatedAt));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult<Report> GetReport(int id)
        {
            var report = _reports.FirstOrDefault(r => r.Id == id);
            if (report == null)
            {
                return NotFound(new { message = "Report not found" });
            }

            return Ok(report);
        }

        [HttpPost]
        [Authorize]
        public ActionResult<Report> CreateReport(CreateReportRequest request)
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
                Id = _reports.Count + 1,
                Reason = request.Reason,
                Type = request.Type,
                ContentId = request.ContentId,
                ReporterUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending
            };

            _reports.Add(report);

            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult<Report> UpdateReport(int id, UpdateReportRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var report = _reports.FirstOrDefault(r => r.Id == id);
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
            report.ModeratorNotes = request.ModeratorNotes;
            report.ResolvedByUserId = userId;
            report.ResolvedAt = DateTime.UtcNow;

            return Ok(report);
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult GetReportStats()
        {
            var stats = new
            {
                TotalReports = _reports.Count,
                PendingReports = _reports.Count(r => r.Status == ReportStatus.Pending),
                InvestigatingReports = _reports.Count(r => r.Status == ReportStatus.Investigating),
                ResolvedReports = _reports.Count(r => r.Status == ReportStatus.Resolved),
                DismissedReports = _reports.Count(r => r.Status == ReportStatus.Dismissed),
                ReportsByType = new
                {
                    ThreadReports = _reports.Count(r => r.Type == ReportType.Thread),
                    PostReports = _reports.Count(r => r.Type == ReportType.Post),
                    MessageReports = _reports.Count(r => r.Type == ReportType.Message)
                }
            };

            return Ok(stats);
        }
    }
} 