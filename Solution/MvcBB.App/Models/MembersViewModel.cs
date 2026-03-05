using Microsoft.AspNetCore.Mvc;
using MvcBB.Shared.Models.Common;
using MvcBB.Shared.Models.User;

namespace MvcBB.App.Models
{
    public class MembersViewModel
    {
        public string Search { get; set; }
        public UserRole? Role { get; set; }
        public string SortBy { get; set; }
        public SortDirection SortDirection { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalMembers { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<UserResponse> Members { get; set; } = Array.Empty<UserResponse>();

        public string GetSortUrl(string column, IUrlHelper url)
        {
            var newDirection = SortBy == column && SortDirection == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;

            return url.Action("Index", "Members", new
            {
                search = Search,
                role = Role,
                sortBy = column,
                sortDirection = newDirection,
                page = 1 // Reset to first page when sorting
            });
        }

        public string GetSortIcon(string column)
        {
            if (SortBy != column)
                return "bi-arrow-down-up";

            return SortDirection == SortDirection.Ascending
                ? "bi-arrow-up"
                : "bi-arrow-down";
        }
    }
} 