namespace MvcBB.Shared.Models.ThreadSubscription
{
    /// <summary>
    /// One user's "watch" on one thread. Created only when the user
    /// explicitly subscribes (no auto-subscribe on reply/create) - the two
    /// notify flags are the delivery channels they picked, and can be
    /// changed later without re-subscribing.
    /// </summary>
    public class ThreadSubscriptionModel
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool NotifyViaMessage { get; set; }
        public bool NotifyViaEmail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
