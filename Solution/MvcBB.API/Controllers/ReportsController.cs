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

        public ReportsController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult<IEnumerable<Report>> GetReports()
        {
            return Ok(_reportRepository.GetAll());
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public ActionResult<Report> GetReport(int id)
        {
            var report = _reportRepository.GetById(id);
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
                Reason = request.Reason,
                Type = request.Type,
                ContentId = request.ContentId,
                ReporterUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending
            };

            report = _reportRepository.Add(report);

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
            report.ModeratorNotes = request.ModeratorNotes;
            report.ResolvedByUserId = userId;
            report.ResolvedAt = DateTime.UtcNow;
            _reportRepository.Update(report);

            return Ok(report);
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
    }
}
