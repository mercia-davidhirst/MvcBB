using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
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

        public AccountController(
            IUserService userService,
            IPostService postService,
            ITurnstileService turnstileService,
            IImageUploadService imageUploadService,
            ISettingsService settingsService)
        {
            _userService = userService;
            _postService = postService;
            _turnstileService = turnstileService;
            _imageUploadService = imageUploadService;
            _settingsService = settingsService;
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
        public async Task<IActionResult> UpdateProfile(int userId, UpdateUserRequest request, IFormFile? avatarFile, bool removeAvatar = false)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                var currentProfile = await _userService.GetUserByIdAsync(userId);

                if (avatarFile != null)
                {
                    request.AvatarUrl = await _imageUploadService.SaveImageAsync(avatarFile, "avatars");
                    _imageUploadService.DeleteImage(currentProfile?.AvatarUrl);
                }
                else if (removeAvatar)
                {
                    _imageUploadService.DeleteImage(currentProfile?.AvatarUrl);
                    request.AvatarUrl = null;
                }
                else
                {
                    request.AvatarUrl = currentProfile?.AvatarUrl;
                }

                await _userService.UpdateProfileAsync(userId, request);
                TempData["Success"] = "Profile updated successfully.";
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
        public async Task<IActionResult> ChangePassword(int userId, ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                await _userService.ChangePasswordAsync(userId, request);
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Policy = "ManageUsers")]
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userService.GetUsersForAdminAsync();
                return View(users);
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

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole([FromForm] UpdateUserRoleRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid request data.";
                return RedirectToAction(nameof(Users));
            }

            try
            {
                // Get the current user's role
                var currentUserRole = User.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(currentUserRole))
                {
                    TempData["Error"] = "Current user role could not be determined.";
                    return RedirectToAction(nameof(Users));
                }

                // Parse the current user's role
                if (!Enum.TryParse<UserRole>(currentUserRole, out var currentRole))
                {
                    TempData["Error"] = "Invalid current user role.";
                    return RedirectToAction(nameof(Users));
                }

                // Prevent role elevation to a higher level than the current user
                if ((int)request.NewRole > (int)currentRole)
                {
                    TempData["Error"] = "Cannot set a role higher than your own role level.";
                    return RedirectToAction(nameof(Users));
                }

                // Get the target user to verify they exist and get their current role
                var targetUser = await _userService.GetUserByIdAsync(request.UserId);
                if (targetUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Users));
                }

                // Prevent modifying users with higher roles
                if (!Enum.TryParse<UserRole>(targetUser.RoleName, out var targetRole))
                {
                    TempData["Error"] = "Invalid target user role.";
                    return RedirectToAction(nameof(Users));
                }

                if ((int)targetRole > (int)currentRole)
                {
                    TempData["Error"] = "Cannot modify users with higher role levels.";
                    return RedirectToAction(nameof(Users));
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

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCustomTitle(int userId, string? customTitle)
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

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnonymizeUser(int userId)
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

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequirePasswordChange(int userId)
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

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserEmail(int userId, string email)
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

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [Authorize(Policy = "ManageUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(int userId, DateTime? lockedUntil)
        {
            try
            {
                await _userService.LockUserAsync(userId, lockedUntil);
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

            return RedirectToAction(nameof(Users));
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
                await _userService.AdminCreateUserAsync(request);
                TempData["Success"] = $"User \"{request.Username}\" created successfully.";
                return RedirectToAction(nameof(Users));
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

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                await _userService.ChangePasswordAsync(userId, request);

                // The cookie's "must_change_password" claim was set at login and
                // won't disappear on its own - re-sign-in with the same claims
                // (and the same expiry/persistence) minus that one so the
                // requirement is actually lifted this session.
                var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var remainingClaims = User.Claims.Where(c => c.Type != "must_change_password");
                var identity = new ClaimsIdentity(remainingClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authResult.Properties);

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

        private async Task SignInUserAsync(UserAuthResponse response)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
                new Claim(ClaimTypes.Name, response.User.Username),
                new Claim(ClaimTypes.Email, response.User.Email),
                new Claim(ClaimTypes.Role, response.User.RoleName),
                new Claim("api_token", response.Token)
            };

            if (response.User.MustChangePassword)
            {
                claims.Add(new Claim("must_change_password", "true"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = response.ExpiresAt
                });
        }

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