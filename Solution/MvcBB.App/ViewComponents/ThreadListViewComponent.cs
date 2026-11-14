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
            // The API already orders sticky-first/most-recently-active and
            // paginates to exactly maxCount (or its own default page size
            // when maxCount is null) - no need to load more than that and
            // re-sort/trim it here.
            var result = await _threadService.GetThreadsAsync(boardId, page: 1, pageSize: maxCount);
            return View(result.Threads);
        }
    }
} 