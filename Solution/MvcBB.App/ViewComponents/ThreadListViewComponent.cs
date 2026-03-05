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

        public async Task<IViewComponentResult> InvokeAsync(int boardId)
        {
            var threads = await _threadService.GetThreadsAsync(boardId);
            return View(threads);
        }
    }
} 