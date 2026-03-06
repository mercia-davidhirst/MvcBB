namespace MvcBB.Shared.Models.ForumThread
{
    public class ThreadListResponse
    {
        public List<ThreadResponse> Threads { get; set; } = new();
        public int TotalThreads { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
