using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Thread
{
    public class UpdateThreadRequest
    {
        [Required(ErrorMessage = "Thread title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
    }
} 