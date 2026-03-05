using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Message
{
    public class CreateMessageRequest
    {
        [Required(ErrorMessage = "Subject is required")]
        [StringLength(100, ErrorMessage = "Subject cannot exceed 100 characters")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(10000, ErrorMessage = "Content cannot exceed 10000 characters")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Recipient user ID is required")]
        public string RecipientUserId { get; set; } = string.Empty;
    }
} 