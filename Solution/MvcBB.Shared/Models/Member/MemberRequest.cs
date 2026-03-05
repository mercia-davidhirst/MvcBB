using System.ComponentModel.DataAnnotations;
using MvcBB.Shared.Models.Common;
using MvcBB.Shared.Models.User;

namespace MvcBB.Shared.Models.Member
{
    public class MemberRequest
    {
        [StringLength(50, ErrorMessage = "Search term cannot exceed 50 characters")]
        public string Search { get; set; } = string.Empty;
        
        public UserRole? Role { get; set; }
        
        [StringLength(20, ErrorMessage = "Sort by field cannot exceed 20 characters")]
        public string SortBy { get; set; } = string.Empty;
        
        public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
        
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;
        
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;
    }
} 