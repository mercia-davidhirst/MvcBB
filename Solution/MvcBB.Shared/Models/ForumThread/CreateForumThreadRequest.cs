using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.ForumThread
{
    public class CreateForumThreadRequest
    {
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        public int BoardId { get; set; }

        [Required]
        public required string Content { get; set; }
    }
}
