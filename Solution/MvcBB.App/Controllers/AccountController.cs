using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.User;
using System.Security.Claims;

namespace MvcBB.App.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
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

            try
            {
                var response = await _userService.LoginAsync(request);
                await SignInUserAsync(response);
                
                TempData["Success"] = "Logged in successfully.";
                return RedirectToLocal(returnUrl);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(request);
            }
        }

        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var response = await _userService.RegisterAsync(request);
                await SignInUserAsync(response);
                
                TempData["Success"] = "Registration successful.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(request);
            }
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var profile = await _userService.GetUserProfileAsync();
                return View(profile);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load profile. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(int userId, UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                await _userService.UpdateProfileAsync(userId, request);
                TempData["Success"] = "Profile updated successfully.";
            }
            catch (Exception ex)
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
            catch (Exception ex)
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
                var users = await _userService.GetUsersAsync();
                return View(users);
            }
            catch (Exception ex)
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update user role.";
            }

            return RedirectToAction(nameof(Users));
        }

        private async Task SignInUserAsync(UserAuthResponse response)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
                new Claim(ClaimTypes.Name, response.User.Username),
                new Claim(ClaimTypes.Email, response.User.Email),
                new Claim(ClaimTypes.Role, response.User.RoleName)
            };

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