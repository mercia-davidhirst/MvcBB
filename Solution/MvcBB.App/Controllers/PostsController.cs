using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Post;

namespace MvcBB.App.Controllers
{
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly IThreadService _threadService;

        public PostsController(IPostService postService, IThreadService threadService)
        {
            _postService = postService;
            _threadService = threadService;
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var post = await _postService.GetPostAsync(id);
                var thread = await _threadService.GetThreadAsync(post.ThreadId);

                // Check if user has permission to edit
                if (post.CreatedByUserId != User.Identity?.Name && 
                    !User.IsInRole("Administrator") && 
                    !User.IsInRole("Moderator"))
                {
                    TempData["Error"] = "You don't have permission to edit this post.";
                    return RedirectToAction("Details", "Threads", new { id = post.ThreadId });
                }

                // Check if thread is locked
                if (thread.IsLocked && !User.IsInRole("Administrator") && !User.IsInRole("Moderator"))
                {
                    TempData["Error"] = "This thread is locked. Posts cannot be edited.";
                    return RedirectToAction("Details", "Threads", new { id = post.ThreadId });
                }

                var updateRequest = new UpdatePostRequest
                {
                    Content = post.Content
                };

                ViewData["Post"] = post;
                ViewData["ThreadTitle"] = thread.Title;
                return View(updateRequest);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load post for editing.";
                return RedirectToAction("Index", "Boards");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var post = await _postService.GetPostAsync(id);
                var thread = await _threadService.GetThreadAsync(post.ThreadId);

                // Check if user has permission to edit
                if (post.CreatedByUserId != User.Identity?.Name && 
                    !User.IsInRole("Administrator") && 
                    !User.IsInRole("Moderator"))
                {
                    TempData["Error"] = "You don't have permission to edit this post.";
                    return RedirectToAction("Details", "Threads", new { id = post.ThreadId });
                }

                // Check if thread is locked
                if (thread.IsLocked && !User.IsInRole("Administrator") && !User.IsInRole("Moderator"))
                {
                    TempData["Error"] = "This thread is locked. Posts cannot be edited.";
                    return RedirectToAction("Details", "Threads", new { id = post.ThreadId });
                }

                await _postService.UpdatePostAsync(id, request);

                TempData["Success"] = "Post updated successfully.";
                return RedirectToAction("Details", "Threads", new { id = post.ThreadId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to update post. Please try again.");
                return View(request);
            }
        }

        [HttpPost]
        [Authorize(Policy = "ManageThreads")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string reason)
        {
            try
            {
                var post = await _postService.GetPostAsync(id);
                await _postService.DeletePostAsync(id);
                TempData["Success"] = "Post deleted successfully.";
                return RedirectToAction("Details", "Threads", new { id = post.ThreadId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to delete post.";
                return RedirectToAction("Details", "Threads", new { id });
            }
        }

        [Authorize]
        public async Task<IActionResult> Quote(int id)
        {
            try
            {
                var post = await _postService.GetPostAsync(id);
                var thread = await _threadService.GetThreadAsync(post.ThreadId);

                if (thread.IsLocked && !User.IsInRole("Administrator") && !User.IsInRole("Moderator"))
                {
                    TempData["Error"] = "This thread is locked. New replies cannot be posted.";
                    return RedirectToAction("Details", "Threads", new { id = thread.Id });
                }

                ViewData["QuotedPost"] = post;
                ViewData["ThreadTitle"] = thread.Title;
                return View("Reply");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load post for quoting.";
                return RedirectToAction("Index", "Boards");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int threadId, CreatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Details", "Threads", new { id = threadId });
            }

            try
            {
                var thread = await _threadService.GetThreadAsync(threadId);

                if (thread.IsLocked && !User.IsInRole("Administrator") && !User.IsInRole("Moderator"))
                {
                    TempData["Error"] = "This thread is locked. New replies cannot be posted.";
                    return RedirectToAction("Details", "Threads", new { id = threadId });
                }

                await _postService.CreatePostAsync(request);

                TempData["Success"] = "Reply posted successfully.";
                return RedirectToAction("Details", "Threads", new { id = threadId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to post reply. Please try again.";
                return RedirectToAction("Details", "Threads", new { id = threadId });
            }
        }
    }
} 