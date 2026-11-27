using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.App.Services;
using MvcBB.Shared.Models.Common;
using MvcBB.Shared.Models.Member;

namespace MvcBB.App.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IThreadService _threadService;
    private readonly IUserService _userService;
    private readonly IBoardService _boardService;
    private readonly ISettingsService _settingsService;
    private readonly OnlineVisitorTracker _onlineVisitors;

    public HomeController(
        ILogger<HomeController> logger,
        IThreadService threadService,
        IUserService userService,
        IBoardService boardService,
        ISettingsService settingsService,
        OnlineVisitorTracker onlineVisitors)
    {
        _onlineVisitors = onlineVisitors;
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
            // Latest threads: the API already returns the 10 most recently
            // active threads across every visible board (by LastPostAt, then
            // CreatedAt), with last poster/date already joined in - so keep
            // that order rather than re-sorting by creation date here.
            var threadResult = await _threadService.GetThreadsAsync(boardId: null, page: 1, pageSize: 10);
            var latestThreads = threadResult.Threads;

            // Get newest members - filtered, sorted and limited to 5 at the
            // data layer, rather than loading every member to sort/take here.
            var membersResult = await _userService.GetMembersAsync(new MemberRequest
            {
                SortBy = "joined",
                SortDirection = SortDirection.Descending,
                Page = 1,
                PageSize = 5
            });
            var newestMembers = membersResult.Members;

            // Calculate statistics - both counts come from the data layer as
            // aggregate COUNTs, rather than loading every member to total
            // them up here.
            var userStats = await _userService.GetUserStatsAsync(activeSinceDays: 7);

            // Total posts from each visible board's stored PostCount (kept up
            // to date on every new thread/reply) - previously this summed the
            // reply counts of only the 10 threads shown above, so it
            // undercounted badly.
            var boards = await _boardService.GetBoardsAsync();
            var stats = new ForumStatistics
            {
                TotalMembers = userStats.TotalMembers,
                TotalThreads = threadResult.TotalThreads,
                TotalPosts = boards.Sum(b => b.PostCount),
                ActiveUsers = userStats.ActiveMembers
            };

            var displaySettings = await _settingsService.GetDisplaySettingsAsync();

            var viewModel = new HomeViewModel
            {
                LatestThreads = latestThreads,
                NewestMembers = newestMembers,
                Stats = stats,
                SiteTitle = displaySettings.SiteTitle,
                WelcomeMessage = displaySettings.WelcomeMessage,
                OnlineVisitors = displaySettings.ShowWhosOnline
                    ? _onlineVisitors.GetSnapshot(displaySettings.OnlineWindowMinutes, DateTime.UtcNow)
                    : null
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
