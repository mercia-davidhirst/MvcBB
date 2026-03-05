using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Board
{
    public class CreateBoardRequest
    {
        [Required(ErrorMessage = "Board name is required")]
        [StringLength(100, ErrorMessage = "Board name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Sort order must be a non-negative number")]
        public int SortOrder { get; set; }
    }
}
