using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Post
{
    public class CreatePostRequest
    {
        [Required(ErrorMessage = "Post content is required")]
        [StringLength(10000, ErrorMessage = "Content cannot exceed 10000 characters")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thread ID is required")]
        public int ThreadId { get; set; }

        public int? QuotedPostId { get; set; }
    }
}
