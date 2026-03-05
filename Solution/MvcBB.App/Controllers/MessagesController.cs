using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
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

        public async Task<IActionResult> Index(string folder = "inbox")
        {
            try
            {
                var messages = await _messageService.GetMessagesAsync(folder);
                ViewData["CurrentFolder"] = folder;
                return View(messages);
            }
            catch (Exception ex)
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load message.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create(string recipientId = null)
        {
            ViewData["RecipientId"] = recipientId;
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
            catch (Exception ex)
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
            catch (Exception ex)
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
                var users = await _userService.GetUsersAsync();
                var matches = users
                    .Where(u => u.Username.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(u => new { id = u.Username, text = u.Username })
                    .Take(10)
                    .ToList();

                return Json(matches);
            }
            catch (Exception ex)
            {
                return Json(new object[] { });
            }
        }
    }
} 