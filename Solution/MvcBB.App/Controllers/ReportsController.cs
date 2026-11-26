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
        private readonly IMvcBBCodeService _bbCodeService;

        public ReportsController(IReportService reportService, IPostService postService, IUserService userService, IMvcBBCodeService bbCodeService)
        {
            _bbCodeService = bbCodeService;
            _reportService = reportService;
            _postService = postService;
            _userService = userService;
        }

        // Polled client-side (site.js) to drive the moderator/administrator
        // "open reports" navbar badge, the same way the unread-PM badge works.
        [HttpGet]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> OpenCount()
        {
            try
            {
                var count = await _reportService.GetOpenCountAsync();
                return Json(new { count });
            }
            catch (ServiceException)
            {
                return Json(new { count = 0 });
            }
        }

        /// <summary>
        /// "Report an issue" form, opened from the bug icon in the navbar.
        /// <paramref name="pageUrl"/> is the page the member was on; they can
        /// choose whether to send it with the report.
        /// </summary>
        [HttpGet]
        public IActionResult ReportIssue(string? pageUrl = null)
        {
            var model = new CreateReportRequest
            {
                Type = ReportType.SiteIssue,
                PageUrl = ReportPageUrl.Normalize(pageUrl)
            };
            ViewData["IncludePageUrl"] = model.PageUrl != null;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportIssue(CreateReportRequest request, bool includePageUrl = false)
        {
            var returnUrl = ReportPageUrl.Normalize(request.PageUrl);
            request.Type = ReportType.SiteIssue;
            request.ContentId = 0;
            request.PageUrl = includePageUrl ? returnUrl : null;

            if (!ModelState.IsValid)
            {
                request.PageUrl = returnUrl;
                ViewData["IncludePageUrl"] = includePageUrl;
                return View(request);
            }

            try
            {
                await _reportService.CreateReportAsync(request);
                TempData["Success"] = "Thanks - your report has been sent to the forum team.";
                return returnUrl != null && Url.IsLocalUrl(returnUrl)
                    ? LocalRedirect(returnUrl)
                    : RedirectToAction("Index", "Home");
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to send your report. Please try again.");
                request.PageUrl = returnUrl;
                ViewData["IncludePageUrl"] = includePageUrl;
                return View(request);
            }
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
                if (report.MessageContent != null)
                {
                    // Rendered with the same BBCode parser and smilies as the
                    // normal message page.
                    ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
                }
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
            if (type == ReportType.SiteIssue)
            {
                return RedirectToAction(nameof(ReportIssue));
            }

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
                    var url = Url.Action("Details", "Threads", new { id = post.ThreadId, postId = post.Id }) + $"#post-{post.Id}";
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
            catch (HttpRequestException ex) when (request.Type == ReportType.Message && ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // The API only accepts message reports from the message's recipient.
                ModelState.AddModelError("", "You can only report private messages that were sent to you.");
                return View(request);
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