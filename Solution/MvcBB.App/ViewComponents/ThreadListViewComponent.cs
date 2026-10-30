using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.App.ViewComponents
{
    public class ThreadListViewComponent : ViewComponent
    {
        private readonly IThreadService _threadService;

        public ThreadListViewComponent(IThreadService threadService)
        {
            _threadService = threadService;
        }

        public async Task<IViewComponentResult> InvokeAsync(int boardId, int? maxCount = null)
        {
            var result = await _threadService.GetThreadsAsync(boardId);
            var ordered = result.Threads
                .OrderByDescending(t => t.IsSticky)
                .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt)
                .AsEnumerable();

            if (maxCount.HasValue)
            {
                ordered = ordered.Take(maxCount.Value);
            }

            return View(ordered);
        }
    }
} 