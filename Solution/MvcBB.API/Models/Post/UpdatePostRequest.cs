using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Post
{
    public class UpdatePostRequest
    {
        [Required]
        public string Content { get; set; }

        [Required]
        public string EditReason { get; set; }
    }
} 