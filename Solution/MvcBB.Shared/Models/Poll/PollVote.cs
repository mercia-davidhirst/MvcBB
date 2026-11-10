namespace MvcBB.Shared.Models.Poll
{
    /// <summary>
    /// One vote per (PollId, UserId) - single-choice, enforced by the
    /// repository rejecting a second vote rather than a unique DB constraint
    /// alone, so the message is consistent across all three backends.
    /// </summary>
    public class PollVote
    {
        public int Id { get; set; }
        public int PollId { get; set; }
        public int PollOptionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
