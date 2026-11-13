using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Member;
using MvcBB.Shared.Models.Message;

namespace MvcBB.App.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;

        public MessagesController(IMessageService messageService, IUserService userService)
        {
            _messageService = messageService;
            _userService = userService;
        }

        // Polled client-side (site.js) to drive the on-screen "new PM" alert -
        // a lightweight JSON endpoint rather than re-rendering the navbar badge.
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                var count = await _messageService.GetUnreadCountAsync();
                return Json(new { count });
            }
            catch (ServiceException)
            {
                return Json(new { count = 0 });
            }
        }

        public async Task<IActionResult> Index(string folder = "inbox")
        {
            try
            {
                var messages = await _messageService.GetMessagesAsync(folder);
                ViewData["CurrentFolder"] = folder;
                return View(messages);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load messages.";
                return View(Array.Empty<MessageResponse>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var message = await _messageService.GetMessageAsync(id);
                return View(message);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load message.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Create(int? recipientId = null)
        {
            if (recipientId.HasValue)
            {
                var recipient = await _userService.GetUserByIdAsync(recipientId.Value);
                if (recipient != null)
                {
                    ViewData["RecipientId"] = recipient.Id;
                    ViewData["RecipientUsername"] = recipient.Username;
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMessageRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                await _messageService.CreateMessageAsync(request);
                TempData["Success"] = "Message sent successfully.";
                return RedirectToAction(nameof(Index), new { folder = "sent" });
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to send message. Please try again.");
                return View(request);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _messageService.DeleteMessageAsync(id);
                TempData["Success"] = "Message deleted successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete message.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetRecipients(string term)
        {
            try
            {
                // Filtered and limited to 10 at the data layer, rather than
                // loading every user to filter/take here on every keystroke.
                var result = await _userService.GetMembersAsync(new MemberRequest
                {
                    Search = term ?? string.Empty,
                    Page = 1,
                    PageSize = 10
                });
                var matches = result.Members
                    .Select(u => new { id = u.Id, text = u.Username })
                    .ToList();

                return Json(matches);
            }
            catch (Exception)
            {
                return Json(new object[] { });
            }
        }
    }
} 