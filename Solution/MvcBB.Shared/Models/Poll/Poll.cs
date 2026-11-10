namespace MvcBB.Shared.Models.Poll
{
    /// <summary>
    /// A single-choice poll attached to a thread when it's first created
    /// (matches classic forum behavior - polls can't be added to a thread
    /// after the fact). One poll per thread.
    /// </summary>
    public class Poll
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public string Question { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Populated on the way in/out by the repository - not every read path
        /// needs the full option list, but Add() always persists these.
        /// </summary>
        public List<PollOption> Options { get; set; } = new();
    }
}
