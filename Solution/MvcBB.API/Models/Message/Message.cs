using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Message
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Content { get; set; }

        public string SenderUserId { get; set; }
        public string RecipientUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        public bool IsDeletedBySender { get; set; }
        public bool IsDeletedByRecipient { get; set; }
    }

} 