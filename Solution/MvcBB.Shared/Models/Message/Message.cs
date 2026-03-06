namespace MvcBB.Shared.Models.Message
{
    public class Message
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string SenderUserId { get; set; } = string.Empty;
        public string RecipientUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
