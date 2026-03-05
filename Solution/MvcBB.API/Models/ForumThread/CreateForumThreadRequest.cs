using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Thread
{
    public class CreateForumThreadRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public int BoardId { get; set; }

        [Required]
        public string Content { get; set; }  // Initial post content
    }
} 