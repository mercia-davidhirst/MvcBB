using MvcBB.Shared.Models.User;
using MvcBB.Shared.Models.Common;

namespace MvcBB.API.Models.Member
{
    public class MemberRequest
    {
        public string Search { get; set; } = string.Empty;
        public UserRole? Role { get; set; }
        public string SortBy { get; set; } = string.Empty;
        public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
} 