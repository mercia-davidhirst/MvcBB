using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.App.Controllers
{
    public class TeamController : Controller
    {
        private readonly IUserService _userService;

        public TeamController(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userService.GetUsersAsync();
                var teamMembers = users.Where(u => 
                    u.Role == UserRole.Administrator || 
                    u.Role == UserRole.Moderator)
                    .OrderByDescending(u => u.Role)
                    .ThenBy(u => u.Username);
                    
                return View(teamMembers);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while fetching the team list.");
                return View(Array.Empty<UserResponse>());
            }
        }
    }
} 