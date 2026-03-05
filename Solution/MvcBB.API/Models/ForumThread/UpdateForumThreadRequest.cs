using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Thread
{
    public class UpdateForumThreadRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }
    }
} 