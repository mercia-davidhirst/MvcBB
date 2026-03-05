using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Models.Common;
using MvcBB.Shared.Models.Member;

namespace MvcBB.App.Controllers
{
    public class MembersController : Controller
    {
        private readonly IUserService _userService;

        public MembersController(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Index(string search = null, UserRole? role = null, 
            string sortBy = null, SortDirection sortDirection = SortDirection.Ascending, int page = 1)
        {
            try
            {
                var response = await _userService.GetMembersAsync(new MemberRequest
                {
                    Search = search,
                    Role = role,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
                    Page = page,
                    PageSize = 20
                });

                var viewModel = new MembersViewModel
                {
                    Search = search,
                    Role = role,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
                    Page = response.Page,
                    PageSize = response.PageSize,
                    TotalMembers = response.TotalMembers,
                    TotalPages = response.TotalPages,
                    Members = response.Members
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                ModelState.AddModelError("", "An error occurred while fetching members. Please try again.");
                return View(new MembersViewModel());
            }
        }
    }
} 