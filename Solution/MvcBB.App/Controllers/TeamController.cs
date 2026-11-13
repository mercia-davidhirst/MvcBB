using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Member;
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
                // Administrators/Moderators are a tiny fraction of the user
                // base in any real community - filtered server-side by role
                // (two small, bounded calls) rather than loading every user
                // just to pick these out.
                var admins = await _userService.GetMembersAsync(new MemberRequest { Role = UserRole.Administrator, SortBy = "username", PageSize = 100 });
                var moderators = await _userService.GetMembersAsync(new MemberRequest { Role = UserRole.Moderator, SortBy = "username", PageSize = 100 });
                var teamMembers = admins.Members
                    .Concat(moderators.Members)
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