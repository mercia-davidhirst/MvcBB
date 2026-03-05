using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Post
{
    public class CreatePostRequest
    {
        [Required]
        public string Content { get; set; }

        [Required]
        public int ThreadId { get; set; }
        
        // Optional quoted post ID
        public int? QuotedPostId { get; set; }
    }
} 