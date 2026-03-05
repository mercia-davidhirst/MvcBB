using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.App.Exceptions;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Post;
using System.Security.Claims;

namespace MvcBB.App.Controllers
{
    public class ThreadsController : Controller
    {
        private readonly IThreadService _threadService;
        private readonly IBoardService _boardService;
        private readonly ILogger<ThreadsController> _logger;

        public ThreadsController(
            IThreadService threadService, 
            IBoardService boardService,
            ILogger<ThreadsController> logger)
        {
            _threadService = threadService;
            _boardService = boardService;
            _logger = logger;
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var thread = await _threadService.GetThreadAsync(id);
                var posts = await _threadService.GetThreadPostsAsync(id);
                
                ViewData["Posts"] = posts;
                return View(thread);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to load thread details for thread {ThreadId}", id);
                TempData["Error"] = "Failed to load thread details. Please try again later.";
                return RedirectToAction("Index", "Boards");
            }
        }

        [Authorize]
        public async Task<IActionResult> QuotePost(int threadId, int postId)
        {
            try
            {
                var thread = await _threadService.GetThreadAsync(threadId);
                var quotedPost = await _threadService.GetPostAsync(postId);
                
                ViewData["QuotedPost"] = quotedPost;
                ViewData["ThreadTitle"] = thread.Title;
                return View("Reply");
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to load quote information for thread {ThreadId}, post {PostId}", threadId, postId);
                TempData["Error"] = "Failed to load quote information.";
                return RedirectToAction(nameof(Details), new { id = threadId });
            }
        }

        [Authorize]
        public async Task<IActionResult> Create(int boardId)
        {
            try
            {
                var board = await _boardService.GetBoardAsync(boardId);
                ViewData["BoardName"] = board.Name;
                ViewData["BoardId"] = boardId;
                return View();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to load board information for board {BoardId}", boardId);
                TempData["Error"] = "Failed to load board information.";
                return RedirectToAction("Index", "Boards");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateForumThreadRequest request)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    var board = await _boardService.GetBoardAsync(request.BoardId);
                    ViewData["BoardName"] = board.Name;
                    ViewData["BoardId"] = request.BoardId;
                }
                catch (ServiceException ex)
                {
                    _logger.LogError(ex, "Failed to load board information for invalid thread creation");
                }
                return View(request);
            }

            try
            {
                var thread = await _threadService.CreateThreadAsync(request);
                TempData["Success"] = "Thread created successfully.";
                return RedirectToAction(nameof(Details), new { id = thread.Id });
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to create thread in board {BoardId}", request.BoardId);
                ModelState.AddModelError("", "Failed to create thread. Please try again.");
                return View(request);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(int threadId, CreatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Details), new { id = threadId });
            }

            try
            {
                await _threadService.CreatePostAsync(threadId, request);
                TempData["Success"] = "Reply posted successfully.";
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to create post in thread {ThreadId}", threadId);
                TempData["Error"] = "Failed to post reply. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = threadId });
        }

        [Authorize(Policy = "ManageThreads")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var thread = await _threadService.GetThreadAsync(id);
                var updateRequest = new UpdateForumThreadRequest
                {
                    Title = thread.Title
                };
                return View(updateRequest);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to load thread {ThreadId} for editing", id);
                TempData["Error"] = "Failed to load thread for editing.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [Authorize(Policy = "ManageThreads")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateForumThreadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                await _threadService.UpdateThreadAsync(id, request);
                TempData["Success"] = "Thread updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to update thread {ThreadId}", id);
                ModelState.AddModelError("", "Failed to update thread. Please try again.");
                return View(request);
            }
        }

        [HttpPost]
        [Authorize(Policy = "ManageThreads")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var thread = await _threadService.GetThreadAsync(id);
                await _threadService.DeleteThreadAsync(id);
                TempData["Success"] = "Thread deleted successfully.";
                return RedirectToAction("Details", "Boards", new { id = thread.BoardId });
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to delete thread {ThreadId}", id);
                TempData["Error"] = "Failed to delete thread.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        private async Task<bool> CanEditPost(int postId)
        {
            if (User.IsInRole("Administrator") || User.IsInRole("Moderator"))
            {
                return true;
            }

            var post = await _threadService.GetPostAsync(postId);
            var userId = User.Identity?.Name;
            return !string.IsNullOrEmpty(userId) && post.CreatedByUserId == userId;
        }

        private async Task<bool> CanEditPostInThread(int threadId)
        {
            if (User.IsInRole("Administrator") || User.IsInRole("Moderator"))
            {
                return true;
            }

            var thread = await _threadService.GetThreadAsync(threadId);
            return !thread.IsLocked;
        }

        [Authorize]
        public async Task<IActionResult> EditPost(int id)
        {
            try
            {
                if (!await CanEditPost(id))
                {
                    TempData["Error"] = "You don't have permission to edit this post.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var post = await _threadService.GetPostAsync(id);
                var thread = await _threadService.GetThreadAsync(post.ThreadId);

                if (!await CanEditPostInThread(thread.Id))
                {
                    TempData["Error"] = "This thread is locked. Posts cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id = thread.Id });
                }

                var updateRequest = new UpdatePostRequest
                {
                    Content = post.Content
                };

                ViewData["Post"] = post;
                ViewData["ThreadTitle"] = thread.Title;
                return View(updateRequest);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to load post {PostId} for editing", id);
                TempData["Error"] = "Failed to load post for editing.";
                return RedirectToAction("Index", "Boards");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, UpdatePostRequest request)
        {
            try
            {
                if (!await CanEditPost(id))
                {
                    TempData["Error"] = "You don't have permission to edit this post.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var post = await _threadService.GetPostAsync(id);
                var thread = await _threadService.GetThreadAsync(post.ThreadId);

                if (!await CanEditPostInThread(thread.Id))
                {
                    TempData["Error"] = "This thread is locked. Posts cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id = thread.Id });
                }

                if (!ModelState.IsValid)
                {
                    ViewData["Post"] = post;
                    ViewData["ThreadTitle"] = thread.Title;
                    return View(request);
                }

                await _threadService.UpdatePostAsync(id, request);
                TempData["Success"] = "Post updated successfully.";
                return RedirectToAction(nameof(Details), new { id = post.ThreadId });
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to update post {PostId}", id);
                TempData["Error"] = "Failed to update post. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
} 