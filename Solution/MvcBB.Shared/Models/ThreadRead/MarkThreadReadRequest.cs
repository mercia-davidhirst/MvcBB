namespace MvcBB.Shared.Models.ThreadRead
{
    public class MarkThreadReadRequest
    {
        /// <summary>The newest post in the thread the member has now seen.</summary>
        public int PostId { get; set; }
    }
}
