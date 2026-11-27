using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.App.Services;
using MvcBB.Shared.Models.Common;
using MvcBB.Shared.Models.Member;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Services;
using System.Security.Claims;

namespace MvcBB.App.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;
        private readonly ITurnstileService _turnstileService;
        private readonly IImageUploadService _imageUploadService;
        private readonly ISettingsService _settingsService;
        private readonly IMvcBBCodeService _bbCodeService;
        private readonly SessionRefresher _sessionRefresher;

        public AccountController(
            IUserService userService,
            IPostService postService,
            ITurnstileService turnstileService,
            IImageUploadService imageUploadService,
            ISettingsService settingsService,
            IMvcBBCodeService bbCodeService,
            SessionRefresher sessionRefresher)
        {
            _sessionRefresher = sessionRefresher;
            _userService = userService;
            _postService = postService;
            _turnstileService = turnstileService;
            _imageUploadService = imageUploadService;
            _settingsService = settingsService;
            _bbCodeService = bbCodeService;
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginRequest request, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                var response = await _userService.LoginAsync(request);
                await SignInUserAsync(response);

                if (response.User.MustChangePassword)
                {
                    return RedirectToAction(nameof(ChangePasswordRequired), new { returnUrl });
                }

                TempData["Success"] = "Logged in successfully.";
                return RedirectToLocal(returnUrl);
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(request);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(request);
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                await _userService.ForgotPasswordAsync(request);
            }
            catch (Exception)
            {
                // Deliberately swallow failures here too - the API already
                // responds identically whether or not the email matched an
                // account, so the user sees the same confirmation either way.
            }

            TempData["Success"] = "If that email is registered, a password reset link has been sent.";
            return RedirectToAction(nameof(Login));
        }

        public IActionResult ResetPassword(string token)
        {
            return View(new ResetPasswordRequest { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                await _userService.ResetPasswordAsync(request);
                TempData["Success"] = "Your password has been reset. You can now log in.";
                return RedirectToAction(nameof(Login));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(request);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "This reset link is invalid or has expired. Please request a new one.");
                return View(request);
            }
        }

        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!(await _settingsService.GetDisplaySettingsAsync()).RegistrationEnabled)
            {
                TempData["Error"] = "Registration is currently disabled.";
                return RedirectToAction(nameof(Login));
            }

            ViewData["TurnstileSiteKey"] = _turnstileService.SiteKey;
            ViewData["PasswordPolicyHint"] = PasswordPolicyValidator.Describe(await _settingsService.GetPasswordPolicyAsync());
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegistrationRequest request, [FromForm(Name = "cf-turnstile-response")] string? turnstileToken)
        {
            if (!(await _settingsService.GetDisplaySettingsAsync()).RegistrationEnabled)
            {
                TempData["Error"] = "Registration is currently disabled.";
                return RedirectToAction(nameof(Login));
            }

            ViewData["TurnstileSiteKey"] = _turnstileService.SiteKey;
            ViewData["PasswordPolicyHint"] = PasswordPolicyValidator.Describe(await _settingsService.GetPasswordPolicyAsync());

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            if (!await _turnstileService.ValidateAsync(turnstileToken, HttpContext.Connection.RemoteIpAddress?.ToString()))
            {
                ModelState.AddModelError("", "CAPTCHA verification failed. Please try again.");
                return View(request);
            }

            try
            {
                var response = await _userService.RegisterAsync(request);
                await SignInUserAsync(response);

                TempData["Success"] = "Registration successful.";
                return RedirectToAction("Index", "Home");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(request);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(request);
            }
        }

        public async Task<IActionResult> Profile(string? username = null)
        {
            var isOwnProfile = string.IsNullOrEmpty(username) ||
                string.Equals(username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase);

            if (isOwnProfile && User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = Url.Action(nameof(Profile)) });
            }

            try
            {
                var profile = isOwnProfile
                    ? await _userService.GetUserProfileAsync()
                    : await _userService.GetUserByUsernameAsync(username!);

                if (profile == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Index", "Members");
                }

                ViewData["IsOwnProfile"] = isOwnProfile;
                ViewData["RecentPosts"] = await _postService.GetPostsByUserAsync(profile.Id, 5);
                ViewData["Smilies"] = await _bbCodeService.GetAvailableSmiliesAsync();
                if (isOwnProfile)
                {
                    ViewData["PasswordPolicyHint"] = PasswordPolicyValidator.Describe(await _settingsService.GetPasswordPolicyAsync());
                }
                return View(profile);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load profile. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateUserRequest request, IFormFile? avatarFile, bool removeAvatar = false)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrEmpty(m)) ?? "Please check your profile details and try again.";
                return RedirectToAction(nameof(Profile));
            }

            // Always the signed-in member's own account - never an id taken
            // from the posted form, which a member could change to anyone's.
            if (!TryGetCurrentUserId(out var userId))
            {
                return await this.RedirectToSessionExpired();
            }

            try
            {
                var currentProfile = await _userService.GetUserByIdAsync(userId);
                string? newAvatarUrl = null;

                if (avatarFile != null)
                {
                    newAvatarUrl = await _imageUploadService.SaveImageAsync(avatarFile, "avatars");
                    request.AvatarUrl = newAvatarUrl;
                }
                else if (removeAvatar)
                {
                    request.AvatarUrl = null;
                }
                else
                {
                    request.AvatarUrl = currentProfile?.AvatarUrl;
                }

                try
                {
                    await _userService.UpdateProfileAsync(userId, request);
                }
                catch
                {
                    // The save was refused (for example a wrong current
                    // password), so keep the old avatar and drop the new upload.
                    _imageUploadService.DeleteImage(newAvatarUrl);
                    throw;
                }

                // Only remove the old avatar file once the profile no longer points at it.
                if (avatarFile != null || removeAvatar)
                {
                    _imageUploadService.DeleteImage(currentProfile?.AvatarUrl);
                }

                TempData["Success"] = "Profile updated successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update profile. Please try again.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Profile));
            }

            if (!TryGetCurrentUserId(out var userId))
            {
                return await this.RedirectToSessionExpired();
            }

            try
            {
                request.KeepSignedIn = SessionCookie.GetRefreshToken(User) != null;
                var newTokens = await _userService.ChangePasswordAsync(userId, request);

                // Changing the password signs the member out everywhere else;
                // swap in the fresh tokens so this session carries on.
                await RefreshSessionAsync(newTokens);
                TempData["Success"] = "Password changed successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to change password. Please try again.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // End a "Remember me" sign-in at the API too, so the refresh token
            // in this cookie can't be used again even if it was copied.
            if (SessionCookie.GetRefreshToken(User) is { } refreshToken)
            {
                await _sessionRefresher.RevokeAsync(refreshToken, HttpContext.RequestAborted);
            }
            // Stop counting them as an online member (they'll show as a guest
            // from the next page on).
            if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value is { } userId)
            {
                HttpContext.RequestServices.GetService<OnlineVisitorTracker>()?.RemoveMember(userId);
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Policy = "ManageUsers")]
        public async Task<IActionResult> Users(string? search = null, UserRole? role = null,
            string? sortBy = null, SortDirection sortDirection = SortDirection.Ascending, int page = 1)
        {
            try
            {
                var response = await _userService.GetMembersForAdminAsync(new MemberRequest
                {
                    Search = search?.Trim() ?? string.Empty,
                    Role = role,
                    SortBy = sortBy ?? string.Empty,
                    SortDirection = sortDirection,
                    Page = Math.Max(1, page),
                    PageSize = AdminUsersViewModel.DefaultPageSize
                });

                return View(new AdminUsersViewModel
                {
                    Search = search,
                    Role = role,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
                    Page = response.Page,
                    PageSize = response.PageSize,
                    TotalUsers = response.TotalMembers,
                    TotalPages = response.TotalPages,
                    Users = response.Members
                });
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load users. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        // The per-user admin page: every detail and action for one user, so
        // the list itself can stay a narrow table with no action column.
        [Authorize(Policy = "ManageUsers")]
        public async Task<IActionResult> ManageUser(int id, string? returnUrl = null)
        {
            try
            {
                var user = await _userService.GetUserForAdminAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToUserList(returnUrl);
                }

                ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
                return View(user);
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load the user. Please try again later.";
                return RedirectToUserList(returnUrl);
            }
        }

        // Admin actions are posted from the per-user page, so land back there
        // (keeping the list's search/page for its "Back to users" link).
        private IActionResult RedirectToManageUser(int userId, string? returnUrl)
        {
            if (userId <= 0)
            {
                return RedirectToUserList(returnUrl);
            }

            return RedirectToAction(nameof(ManageUser), new { id = userId, returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
        }

        private IActionResult RedirectToUserList(string? returnUrl) =>
            Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Users));

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole([FromForm] UpdateUserRoleRequest request, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid request data.";
                return RedirectToManageUser(request.UserId, returnUrl);
            }

            try
            {
                // Get the current user's role
                var currentUserRole = User.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(currentUserRole))
                {
                    TempData["Error"] = "Current user role could not be determined.";
                    return RedirectToManageUser(request.UserId, returnUrl);
                }

                // Parse the current user's role
                if (!Enum.TryParse<UserRole>(currentUserRole, out var currentRole))
                {
                    TempData["Error"] = "Invalid current user role.";
                    return RedirectToManageUser(request.UserId, returnUrl);
                }

                // Prevent role elevation to a higher level than the current user
                if ((int)request.NewRole > (int)currentRole)
                {
                    TempData["Error"] = "Cannot set a role higher than your own role level.";
                    return RedirectToManageUser(request.UserId, returnUrl);
                }

                // Get the target user to verify they exist and get their current role
                var targetUser = await _userService.GetUserByIdAsync(request.UserId);
                if (targetUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToManageUser(request.UserId, returnUrl);
                }

                // Prevent modifying users with higher roles
                if (!Enum.TryParse<UserRole>(targetUser.RoleName, out var targetRole))
                {
                    TempData["Error"] = "Invalid target user role.";
                    return RedirectToManageUser(request.UserId, returnUrl);
                }

                if ((int)targetRole > (int)currentRole)
                {
                    TempData["Error"] = "Cannot modify users with higher role levels.";
                    return RedirectToManageUser(request.UserId, returnUrl);
                }

                await _userService.UpdateUserRoleAsync(request.UserId, request.NewRole);
                TempData["Success"] = "User role updated successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update user role.";
            }

            return RedirectToManageUser(request.UserId, returnUrl);
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCustomTitle(int userId, string? customTitle, string? returnUrl = null)
        {
            try
            {
                await _userService.SetCustomTitleAsync(userId, customTitle);
                TempData["Success"] = "Custom title updated successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update custom title.";
            }

            return RedirectToManageUser(userId, returnUrl);
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnonymizeUser(int userId, string? returnUrl = null)
        {
            try
            {
                await _userService.AnonymizeUserAsync(userId);
                TempData["Success"] = "User anonymized successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to anonymize user.";
            }

            return RedirectToManageUser(userId, returnUrl);
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequirePasswordChange(int userId, string? returnUrl = null)
        {
            try
            {
                await _userService.RequirePasswordChangeAsync(userId);
                TempData["Success"] = "The user will be asked to set a new password the next time they log in.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to flag the user for a password change.";
            }

            return RedirectToManageUser(userId, returnUrl);
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserEmail(int userId, string email, string? returnUrl = null)
        {
            try
            {
                await _userService.UpdateUserEmailAsync(userId, email);
                TempData["Success"] = "Email updated successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update email.";
            }

            return RedirectToManageUser(userId, returnUrl);
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(int userId, DateTime? lockedUntil, string? returnUrl = null)
        {
            try
            {
                // The lock form's datetime-local input is in the forum's time
                // zone; the API stores and compares UTC.
                await _userService.LockUserAsync(userId, await HttpContext.ForumTimeToUtcAsync(lockedUntil));
                TempData["Success"] = lockedUntil.HasValue ? "User locked." : "User unlocked.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update the account lock.";
            }

            return RedirectToManageUser(userId, returnUrl);
        }

        [Authorize(Policy = "ManageUsers")]
        public async Task<IActionResult> CreateUser()
        {
            ViewData["PasswordPolicyHint"] = PasswordPolicyValidator.Describe(await _settingsService.GetPasswordPolicyAsync());
            return View(new AdminCreateUserRequest());
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(AdminCreateUserRequest request)
        {
            ViewData["PasswordPolicyHint"] = PasswordPolicyValidator.Describe(await _settingsService.GetPasswordPolicyAsync());

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var created = await _userService.AdminCreateUserAsync(request);
                TempData["Success"] = $"User \"{request.Username}\" created successfully.";
                return created != null && created.Id > 0
                    ? RedirectToAction(nameof(ManageUser), new { id = created.Id })
                    : RedirectToAction(nameof(Users));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(request);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to create user. Please try again.");
                return View(request);
            }
        }

        [Authorize]
        public async Task<IActionResult> ChangePasswordRequired(string? returnUrl = null)
        {
            if (User.FindFirst("must_change_password") == null)
            {
                return RedirectToLocal(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["PasswordPolicyHint"] = PasswordPolicyValidator.Describe(await _settingsService.GetPasswordPolicyAsync());
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordRequired(ChangePasswordRequest request, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["PasswordPolicyHint"] = PasswordPolicyValidator.Describe(await _settingsService.GetPasswordPolicyAsync());

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            if (!TryGetCurrentUserId(out var userId))
            {
                return await this.RedirectToSessionExpired();
            }

            try
            {
                request.KeepSignedIn = SessionCookie.GetRefreshToken(User) != null;
                var newTokens = await _userService.ChangePasswordAsync(userId, request);

                // The cookie's "must_change_password" claim was set at login and
                // won't disappear on its own - re-sign-in without it so the
                // requirement is actually lifted this session, carrying the
                // fresh API token (the old one stopped working with the change).
                await RefreshSessionAsync(newTokens, removeMustChangePassword: true);

                TempData["Success"] = "Password changed successfully.";
                return RedirectToLocal(returnUrl);
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(request);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to change password. Please check your current password and try again.");
                return View(request);
            }
        }

        // "Remember me" (a refresh token in the response) makes a persistent
        // cookie that is renewed quietly; otherwise it's a browser-session
        // cookie - see SessionCookie.
        private async Task SignInUserAsync(UserAuthResponse response)
        {
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                SessionCookie.CreatePrincipal(response),
                SessionCookie.CreateProperties(SessionTokens.From(response)));
        }

        // Re-issues the cookie with the same claims and persistence, swapping
        // in new API tokens and optionally dropping the must_change_password
        // flag.
        private async Task RefreshSessionAsync(SessionTokens? newTokens, bool removeMustChangePassword = false)
        {
            if (newTokens == null && !removeMustChangePassword)
            {
                return;
            }

            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = authResult.Properties ?? new AuthenticationProperties();
            ClaimsPrincipal principal;
            if (newTokens != null)
            {
                principal = SessionCookie.WithTokens(User, newTokens, removeMustChangePassword);
                SessionCookie.ApplyExpiry(properties, newTokens);
            }
            else
            {
                var claims = User.Claims.Where(c => c.Type != SessionCookie.MustChangePasswordClaim).ToList();
                principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
        }

        private bool TryGetCurrentUserId(out int userId) =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId) && userId > 0;

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
} 