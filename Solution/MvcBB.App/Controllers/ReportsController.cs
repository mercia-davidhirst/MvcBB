using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Report;

namespace MvcBB.App.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IPostService _postService;
        private readonly IUserService _userService;

        public ReportsController(IReportService reportService, IPostService postService, IUserService userService)
        {
            _reportService = reportService;
            _postService = postService;
            _userService = userService;
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
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
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
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
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
                if (request.Type == ReportType.Post)
                {
                    // ContentId is the post's own id here, not a thread id - look up
                    // which thread it belongs to so the redirect actually lands
                    // somewhere instead of treating the post id as a thread id.
                    var post = await _postService.GetPostAsync(request.ContentId);
                    var url = Url.Action("Details", "Threads", new { id = post.ThreadId }) + $"#post-{post.Id}";
                    return Redirect(url);
                }

                if (request.Type == ReportType.User)
                {
                    var reportedUser = await _userService.GetUserByIdAsync(request.ContentId);
                    return reportedUser != null
                        ? RedirectToAction("Profile", "Account", new { username = reportedUser.Username })
                        : RedirectToAction("Index", "Home");
                }

                return request.Type switch
                {
                    ReportType.Thread => RedirectToAction("Details", "Threads", new { id = request.ContentId }),
                    ReportType.Message => RedirectToAction("Details", "Messages", new { id = request.ContentId }),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
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
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update report status.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
} 