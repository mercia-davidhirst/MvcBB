using MvcBB.Shared.Models.User;

namespace MvcBB.API.Models.Member
{
    public class MemberResponse
    {
        public List<UserResponse> Members { get; set; } = new();
        public int TotalMembers { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
} 