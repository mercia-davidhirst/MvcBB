using Microsoft.AspNetCore.Mvc;
using MvcBB.Shared.Models.Common;
using MvcBB.Shared.Models.User;

namespace MvcBB.App.Models
{
    /// <summary>
    /// The admin User Management list (Account/Users): one page of users plus
    /// the current search/role/sort, so every link on the page (sort headers,
    /// pager, "back to list" on the per-user page) can carry them along.
    /// </summary>
    public class AdminUsersViewModel
    {
        public const int DefaultPageSize = 25;

        public string? Search { get; set; }
        public UserRole? Role { get; set; }
        public string? SortBy { get; set; }
        public SortDirection SortDirection { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
        public int TotalUsers { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<UserResponse> Users { get; set; } = Array.Empty<UserResponse>();

        public bool IsFiltered => !string.IsNullOrWhiteSpace(Search) || Role.HasValue;

        public string? GetPageUrl(int page, IUrlHelper url) =>
            url.Action("Users", "Account", new
            {
                search = Search,
                role = Role,
                sortBy = SortBy,
                sortDirection = SortDirection,
                page
            });

        public string? GetSortUrl(string column, IUrlHelper url)
        {
            var newDirection = string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase) && SortDirection == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;

            return url.Action("Users", "Account", new
            {
                search = Search,
                role = Role,
                sortBy = column,
                sortDirection = newDirection,
                page = 1
            });
        }

        public string GetSortIcon(string column)
        {
            if (!string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase))
                return "bi-arrow-down-up";

            return SortDirection == SortDirection.Ascending ? "bi-arrow-up" : "bi-arrow-down";
        }

        /// <summary>
        /// Page numbers to show in the pager: the first and last page, plus a
        /// window around the current one. Null marks a gap ("…"). Keeps the
        /// pager short on a forum with thousands of members.
        /// </summary>
        public IEnumerable<int?> GetPagerPages(int window = 2)
        {
            int? last = null;
            for (var i = 1; i <= TotalPages; i++)
            {
                if (i == 1 || i == TotalPages || Math.Abs(i - Page) <= window)
                {
                    if (last.HasValue && i - last.Value > 1)
                    {
                        yield return null;
                    }

                    yield return i;
                    last = i;
                }
            }
        }
    }
}
