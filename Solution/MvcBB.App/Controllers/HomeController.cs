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

    public HomeController(
        ILogger<HomeController> logger,
        IThreadService threadService,
        IUserService userService,
        IBoardService boardService)
    {
        _logger = logger;
        _threadService = threadService;
        _userService = userService;
        _boardService = boardService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Get latest threads
            var threads = await _threadService.GetThreadsAsync();
            var latestThreads = threads
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
                TotalThreads = threads.Count(),
                TotalPosts = threads.Sum(t => t.ReplyCount),
                ActiveUsers = users.Count(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-7))
            };

            var viewModel = new HomeViewModel
            {
                LatestThreads = latestThreads,
                NewestMembers = newestMembers,
                Stats = stats
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
