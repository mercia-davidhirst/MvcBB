using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.App.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Index(ReportStatus? status = null)
        {
            try
            {
                var reports = await _reportService.GetReportsAsync(status);
                ViewData["CurrentStatus"] = status;
                return View(reports);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load reports.";
                return View(Array.Empty<ReportResponse>());
            }
        }

        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var report = await _reportService.GetReportAsync(id);
                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load report details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create(ReportType type, int contentId)
        {
            var request = new CreateReportRequest
            {
                Type = type,
                ContentId = contentId
            };
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReportRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                await _reportService.CreateReportAsync(request);
                TempData["Success"] = "Report submitted successfully.";

                // Redirect back to the appropriate content
                return request.Type switch
                {
                    ReportType.Thread => RedirectToAction("Details", "Threads", new { id = request.ContentId }),
                    ReportType.Post => RedirectToAction("Details", "Threads", new { id = request.ContentId }), // We'll need to find the thread ID
                    ReportType.Message => RedirectToAction("Details", "Messages", new { id = request.ContentId }),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to submit report. Please try again.");
                return View(request);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Moderator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, UpdateReportRequest request)
        {
            try
            {
                await _reportService.UpdateReportAsync(id, request);
                TempData["Success"] = "Report status updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update report status.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
} 