using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Message;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.Report;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.InMemory
{
    /// <summary>
    /// Single in-memory store shared by all InMemory repositories.
    /// </summary>
    public class InMemoryStore
    {
        public List<User> Users { get; } = new();
        public List<Board> Boards { get; } = new();
        public List<ForumThread> Threads { get; } = new();
        public List<Post> Posts { get; } = new();
        public List<Message> Messages { get; } = new();
        public List<Report> Reports { get; } = new();
    }
}
