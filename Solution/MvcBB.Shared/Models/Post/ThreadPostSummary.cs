namespace MvcBB.Shared.Models.Post
{
    /// <summary>
    /// Per-thread post aggregate used when rendering a page of threads, so the
    /// participant count and every post id in the thread (needed to batch-look-up
    /// like counts) come from one query across the whole page instead of one
    /// GetByThreadId call per thread - see IPostRepository.GetThreadSummaries.
    /// </summary>
    public class ThreadPostSummary
    {
        public int ParticipantCount { get; set; }
        public IReadOnlyList<int> PostIds { get; set; } = Array.Empty<int>();
    }
}
