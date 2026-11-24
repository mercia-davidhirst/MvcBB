using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.App.Controllers
{
    /// <summary>
    /// Admin > Email Users: write an email, check who it goes to and who it's
    /// from on a confirmation step, then queue it. The API sends it in the
    /// background in throttled batches; Details shows progress.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    public class AdminEmailsController : Controller
    {
        private readonly IAdminEmailService _adminEmailService;
        private readonly IUserService _userService;

        public AdminEmailsController(IAdminEmailService adminEmailService, IUserService userService)
        {
            _adminEmailService = adminEmailService;
            _userService = userService;
        }

        // userId: opened from a user's admin page ("Email this user").
        public async Task<IActionResult> Index(int? userId = null)
        {
            var model = new AdminEmailComposeViewModel();
            try
            {
                if (userId.HasValue)
                {
                    var user = await _userService.GetUserForAdminAsync(userId.Value);
                    if (user != null)
                    {
                        model.Audience = AdminEmailAudience.SingleUser;
                        model.Username = user.Username;
                    }
                }

                return await ComposeView(model);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
        }

        // "Back to edit" from the confirmation step.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminEmailComposeViewModel model)
        {
            ModelState.Clear();
            try
            {
                return await ComposeView(model);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(AdminEmailComposeViewModel model)
        {
            try
            {
                var userId = await ResolveUserIdAsync(model);
                if (!ModelState.IsValid)
                {
                    return await ComposeView(model);
                }

                model.Preview = await _adminEmailService.PreviewAsync(model.ToRequest(userId));
                return View("Confirm", model);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return await ComposeView(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Couldn't check who this email would go to. Please try again.");
                return await ComposeView(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(AdminEmailComposeViewModel model)
        {
            try
            {
                var userId = await ResolveUserIdAsync(model);
                if (!ModelState.IsValid)
                {
                    return await ComposeView(model);
                }

                var email = await _adminEmailService.SendAsync(model.ToRequest(userId));
                TempData["Success"] = $"Your email is queued for {email.RecipientCount} member(s). It goes out in batches in the background, so you can leave this page.";
                return RedirectToAction(nameof(Details), new { id = email.Id });
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return await ComposeView(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to queue the email. Please try again.");
                return await ComposeView(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTest(AdminEmailComposeViewModel model)
        {
            try
            {
                var userId = await ResolveUserIdAsync(model);
                if (!ModelState.IsValid)
                {
                    return await ComposeView(model);
                }

                TempData["Success"] = await _adminEmailService.SendTestAsync(model.ToRequest(userId));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to send the test email. Please try again.");
            }

            try
            {
                return await ComposeView(model);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var email = await _adminEmailService.GetAsync(id);
                if (email == null)
                {
                    TempData["Error"] = "Email not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (email.Audience == AdminEmailAudience.SingleUser && email.UserId.HasValue)
                {
                    ViewData["RecipientUsername"] = (await _userService.GetUserForAdminAsync(email.UserId.Value))?.Username;
                }

                return View(email);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load the email. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // The form takes a username for "one member"; the API wants the id.
        private async Task<int?> ResolveUserIdAsync(AdminEmailComposeViewModel model)
        {
            if (model.Audience == AdminEmailAudience.Role && model.Role == null)
            {
                ModelState.AddModelError(nameof(model.Role), "Choose a role.");
            }

            if (model.Audience != AdminEmailAudience.SingleUser)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ModelState.AddModelError(nameof(model.Username), "Enter the member's username.");
                return null;
            }

            var user = await _userService.GetUserByUsernameAsync(model.Username.Trim());
            if (user == null)
            {
                ModelState.AddModelError(nameof(model.Username), $"No member is called \"{model.Username.Trim()}\".");
                return null;
            }

            return user.Id;
        }

        private async Task<IActionResult> ComposeView(AdminEmailComposeViewModel model)
        {
            model.Preview = null;
            try
            {
                model.AdminEmailAddress = (await _userService.GetUserProfileAsync()).Email;
                model.RecentEmails = await _adminEmailService.GetRecentAsync();
            }
            catch (SessionExpiredException)
            {
                throw;
            }
            catch (Exception)
            {
                // The form still works without the send log; say so rather than fail the page.
                ViewData["LogError"] = "The send log couldn't be loaded.";
            }

            return View("Index", model);
        }
    }
}
