using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;

namespace MvcBB.App.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IThreadService _threadService;
    private readonly IUserService _userService;
    private readonly IBoardService _boardService;
    private readonly ISettingsService _settingsService;

    public HomeController(
        ILogger<HomeController> logger,
        IThreadService threadService,
        IUserService userService,
        IBoardService boardService,
        ISettingsService settingsService)
    {
        _logger = logger;
        _threadService = threadService;
        _userService = userService;
        _boardService = boardService;
        _settingsService = settingsService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Get latest threads (first page, 10 per page for home)
            var threadResult = await _threadService.GetThreadsAsync(boardId: null, page: 1, pageSize: 10);
            var latestThreads = threadResult.Threads
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToList();

            // Get newest members
            var users = await _userService.GetUsersAsync();
            var newestMembers = users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToList();

            // Calculate statistics
            var stats = new ForumStatistics
            {
                TotalMembers = users.Count(),
                TotalThreads = threadResult.TotalThreads,
                TotalPosts = threadResult.Threads.Sum(t => t.ReplyCount),
                ActiveUsers = users.Count(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-7))
            };

            var displaySettings = await _settingsService.GetDisplaySettingsAsync();

            var viewModel = new HomeViewModel
            {
                LatestThreads = latestThreads,
                NewestMembers = newestMembers,
                Stats = stats,
                SiteTitle = displaySettings.SiteTitle,
                WelcomeMessage = displaySettings.WelcomeMessage
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page data");
            return View(new HomeViewModel());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
