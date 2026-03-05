using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.ForumThread
{
    public class UpdateForumThreadRequest
    {
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }
    }
}
