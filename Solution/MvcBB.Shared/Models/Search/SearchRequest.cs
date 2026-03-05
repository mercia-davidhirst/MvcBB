using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Search
{
    public class SearchRequest
    {
        [Required(ErrorMessage = "Search query is required")]
        [StringLength(100, ErrorMessage = "Search query cannot exceed 100 characters")]
        public string Query { get; set; } = string.Empty;

        [Required(ErrorMessage = "Search type is required")]
        [RegularExpression("^(all|threads|posts|users)$", ErrorMessage = "Invalid search type. Must be 'all', 'threads', 'posts', or 'users'")]
        public string Type { get; set; } = "all";

        public int? BoardId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;
    }
} 