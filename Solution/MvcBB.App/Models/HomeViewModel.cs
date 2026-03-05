using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.User;

namespace MvcBB.App.Models
{
    public class HomeViewModel
    {
        public IEnumerable<ThreadResponse> LatestThreads { get; set; } = Array.Empty<ThreadResponse>();
        public IEnumerable<UserResponse> NewestMembers { get; set; } = Array.Empty<UserResponse>();
        public ForumStatistics Stats { get; set; } = new();
    }

    public class ForumStatistics
    {
        public int TotalMembers { get; set; }
        public int TotalThreads { get; set; }
        public int TotalPosts { get; set; }
        public int ActiveUsers { get; set; }
    }
} 