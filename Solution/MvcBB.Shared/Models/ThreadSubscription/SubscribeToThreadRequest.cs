namespace MvcBB.Shared.Models.ThreadSubscription
{
    public class SubscribeToThreadRequest
    {
        public bool NotifyViaMessage { get; set; } = true;
        public bool NotifyViaEmail { get; set; }
    }
}
