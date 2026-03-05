using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Post
{
    public class UpdatePostRequest
    {
        [Required(ErrorMessage = "Post content is required")]
        [StringLength(10000, ErrorMessage = "Content cannot exceed 10000 characters")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string EditReason { get; set; }
    }
}
