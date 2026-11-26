using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Models;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Poll;
using MvcBB.Shared.Models.Post;
using System.Security.Claims;

namespace MvcBB.App.Controllers
{
    public class ThreadsController : Controller
    {
        private readonly IThreadService _threadService;
        private readonly IBoardService _boardService;
        private readonly IMvcBBCodeService _bbCodeService;
        private readonly IImageUploadService _imageUploadService;
        private readonly ILogger<ThreadsController> _logger;

        public ThreadsController(
            IThreadService threadService,
            IBoardService boardService,
            IMvcBBCodeService bbCodeService,
            IImageUploadService imageUploadService,
            ILogger<ThreadsController> logger)
        {
            _threadService = threadService;
            _boardService = boardService;
            _bbCodeService = bbCodeService;
            _imageUploadService = imageUploadService;
            _logger = logger;
        }

        /// <summary>
        /// A page of a discussion. With postId, the page holding that post
        /// (link to it as Details?postId=N#post-N, or use GoToPost); with
        /// last=true, the last page. With none of page/postId/last, a member
        /// whose profile has "jump to first unread" on gets the page holding
        /// their first unread post; everyone else gets page 1.
        /// </summary>
        public async Task<IActionResult> Details(int id, int? page = null, int? postId = null, bool last = false)
        {
            try
            {
                var thread = await _threadService.GetThreadAsync(id);
                var openAtUnread = page == null && postId == null && !last && thread.ReadState?.AutoJumpToUnread == true;
                var postPage = await _threadService.GetThreadPostPageAsync(id, page, postId, openAtUnread, last);
                var posts = postPage.Posts;

                ViewData["Posts"] = posts;
                ViewData["PostPage"] = postPage;
                ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
                // Every real member @mentioned on the page, so the view can link them.
                ViewData["MentionedUsernames"] = posts
                    .SelectMany(p => p.MentionedUsernames)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return View(thread);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to load thread details for thread {ThreadId}", id);
                TempData["Error"] = "Failed to load thread details. Please try again later.";
                return RedirectToAction("Index", "Boards");
            }
        }

        /// <summary>
        /// Opens whichever page of its discussion a post is on, scrolled to the
        /// post - for links that only know the post (search results, a
        /// member's recent posts, reports, notifications).
        /// </summary>
        public async Task<IActionResult> GoToPost(int postId)
        {
            try
            {
                var post = await _threadService.GetPostAsync(postId);
                return Redirect(PostUrl(post.ThreadId, post.Id));
            }
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Failed to find post {PostId}", postId);
                TempData["Error"] = "That post is no longer available.";
                return RedirectToAction("Index", "Boards");
            }
        }

        /// <summary>The page of the discussion holding the post, scrolled to it.</summary>
        private string PostUrl(int threadId, int postId) =>
            Url.Action(nameof(Details), new { id = threadId, postId }) + $"#post-{postId}";

        [Authorize]
        public async Task<IActionResult> Reply(int threadId)
        {
            try
            {
                var thread = await _threadService.GetThreadAsync(threadId);

                ViewData["ThreadTitle"] = thread.Title;
                ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
                return View(new CreatePostRequest { ThreadId = threadId });
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to load thread {ThreadId} for reply", threadId);
                TempData["Error"] = "Failed to load thread. Please try again later.";
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
                ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
                return View("Reply", new CreatePostRequest { ThreadId = threadId, QuotedPostId = postId });
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
                ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
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
        public async Task<IActionResult> Create(CreateForumThreadRequest request, string? pollQuestion, List<string>? pollOptions)
        {
            // Poll fields are bound separately (not as request.Poll) so that an
            // unused poll section never trips [Required]/[MinLength] validation
            // on CreatePollRequest - only build it when a question was actually entered.
            if (!string.IsNullOrWhiteSpace(pollQuestion))
            {
                var options = (pollOptions ?? new List<string>())
                    .Select(o => o?.Trim())
                    .Where(o => !string.IsNullOrEmpty(o))
                    .Cast<string>()
                    .ToList();

                if (options.Count < 2)
                {
                    ModelState.AddModelError("", "A poll needs at least 2 non-empty options.");
                }
                else
                {
                    request.Poll = new CreatePollRequest { Question = pollQuestion.Trim(), Options = options };
                }
            }

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
                ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
                return View(request);
            }

            try
            {
                var thread = await _threadService.CreateThreadAsync(request);
                TempData[DraftKeys.ClearDraftTempDataKey] = DraftKeys.NewThread(request.BoardId);
                if (thread == null)
                {
                    TempData["Success"] = "Thanks! Your discussion will appear once a moderator has approved it. You'll get a notification when they have.";
                    return RedirectToAction("Details", "Boards", new { id = request.BoardId });
                }

                TempData["Success"] = "Thread created successfully.";
                return RedirectToAction(nameof(Details), new { id = thread.Id });
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
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
        public async Task<IActionResult> CreatePost(int threadId, CreatePostRequest request, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Details), new { id = threadId });
            }

            try
            {
                // The image URL only ever comes from a file uploaded here - never
                // from the posted form, where it could be any path.
                request.ImageUrl = imageFile != null
                    ? await _imageUploadService.SaveImageAsync(imageFile, "posts")
                    : null;

                var post = await _threadService.CreatePostAsync(threadId, request);
                TempData[DraftKeys.ClearDraftTempDataKey] = DraftKeys.Reply(threadId);
                if (post == null)
                {
                    TempData["Success"] = "Thanks! Your reply will appear once a moderator has approved it. You'll get a notification when they have.";
                }
                else
                {
                    TempData["Success"] = "Reply posted successfully.";
                    return Redirect(PostUrl(threadId, post.Id));
                }
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to create post in thread {ThreadId}", threadId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = threadId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikePost(int id, int threadId)
        {
            try
            {
                await _threadService.ToggleLikeAsync(id);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to toggle like on post {PostId}", id);
                TempData["Error"] = ex.Message;
            }

            return Redirect(PostUrl(threadId, id));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VotePoll(int threadId, int pollOptionId)
        {
            try
            {
                await _threadService.VotePollAsync(threadId, pollOptionId);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to cast poll vote in thread {ThreadId}", threadId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = threadId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Watch(int threadId, bool notifyViaMessage, bool notifyViaEmail)
        {
            try
            {
                await _threadService.SubscribeToThreadAsync(threadId, notifyViaMessage, notifyViaEmail);
                TempData["Success"] = "You're now watching this thread.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to subscribe to thread {ThreadId}", threadId);
                TempData["Error"] = "Failed to subscribe to thread.";
            }

            return RedirectToAction(nameof(Details), new { id = threadId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unwatch(int threadId)
        {
            try
            {
                await _threadService.UnsubscribeFromThreadAsync(threadId);
                TempData["Success"] = "You've stopped watching this thread.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from thread {ThreadId}", threadId);
                TempData["Error"] = "Failed to unsubscribe from thread.";
            }

            return RedirectToAction(nameof(Details), new { id = threadId });
        }

        /// <summary>
        /// Called from the discussion page's script as posts scroll into view,
        /// with the newest post the member has now seen. Returns a bare status
        /// code (no redirect) since nothing on the page waits for it.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int threadId, int postId)
        {
            try
            {
                await _threadService.MarkThreadReadAsync(threadId, postId);
                return NoContent();
            }
            catch (SessionExpiredException)
            {
                return Unauthorized();
            }
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Failed to save read position for thread {ThreadId}, post {PostId}", threadId, postId);
                return StatusCode(StatusCodes.Status502BadGateway);
            }
        }

        [Authorize(Policy = "ManageThreads")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var thread = await _threadService.GetThreadAsync(id);
                var updateRequest = new UpdateForumThreadRequest
                {
                    Title = thread.Title,
                    IsSticky = thread.IsSticky,
                    IsLocked = thread.IsLocked
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
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
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
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
                    Content = post.Content,
                    ImageUrl = post.ImageUrl
                };

                ViewData["Post"] = post;
                ViewData["ThreadTitle"] = thread.Title;
                ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
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
        public async Task<IActionResult> EditPost(int id, UpdatePostRequest request, IFormFile? imageFile, bool removeImage = false)
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
                    ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
                    return View(request);
                }

                if (imageFile != null)
                {
                    request.ImageUrl = await _imageUploadService.SaveImageAsync(imageFile, "posts");
                    _imageUploadService.DeleteImage(post.ImageUrl);
                }
                else if (removeImage)
                {
                    _imageUploadService.DeleteImage(post.ImageUrl);
                    request.ImageUrl = null;
                }
                else
                {
                    request.ImageUrl = post.ImageUrl;
                }

                await _threadService.UpdatePostAsync(id, request);
                TempData[DraftKeys.ClearDraftTempDataKey] = DraftKeys.Edit(id);
                TempData["Success"] = "Post updated successfully.";
                return Redirect(PostUrl(post.ThreadId, id));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Failed to update post {PostId}", id);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
} 