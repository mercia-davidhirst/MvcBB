using System.Security.Claims;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Post;

namespace MvcBB.API.Services
{
    /// <summary>
    /// The one place that decides who can see a board, a thread or a post.
    /// Every controller that reads or writes forum content goes through this
    /// instead of repeating its own (slightly different) checks.
    ///
    /// Rules:
    /// - A board is visible if it's active (or the caller is an Administrator)
    ///   and it's either public or the caller is signed in (Board.IsVisibleTo).
    /// - A thread or post is visible only if its board is. A thread whose board
    ///   no longer exists is treated as hidden.
    /// - A deleted post's content is only shown to Administrators and
    ///   Moderators; everyone else just sees that it was deleted.
    /// </summary>
    public interface IContentVisibilityService
    {
        bool IsStaff { get; }
        bool CanSeeBoard(Board? board);
        bool CanSeeBoard(int boardId);
        bool CanSeeThread(ForumThread? thread);
        bool CanSeeThread(int threadId);
        bool CanSeePostContent(Post post);
    }

    public class ContentVisibilityService : IContentVisibilityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBoardRepository _boardRepository;
        private readonly IForumThreadRepository _threadRepository;

        // Per-request caches - a page of posts usually shares one thread/board.
        private readonly Dictionary<int, bool> _boardVisibility = new();
        private readonly Dictionary<int, bool> _threadVisibility = new();

        public ContentVisibilityService(
            IHttpContextAccessor httpContextAccessor,
            IBoardRepository boardRepository,
            IForumThreadRepository threadRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _boardRepository = boardRepository;
            _threadRepository = threadRepository;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        private bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        private bool IsAdmin => User?.IsInRole("Administrator") == true;

        public bool IsStaff => IsAdmin || User?.IsInRole("Moderator") == true;

        public bool CanSeeBoard(Board? board) =>
            board != null && board.IsVisibleTo(IsAuthenticated, IsAdmin);

        public bool CanSeeBoard(int boardId)
        {
            if (!_boardVisibility.TryGetValue(boardId, out var visible))
            {
                visible = CanSeeBoard(_boardRepository.GetById(boardId));
                _boardVisibility[boardId] = visible;
            }
            return visible;
        }

        public bool CanSeeThread(ForumThread? thread)
        {
            if (thread == null)
            {
                return false;
            }

            var visible = CanSeeBoard(thread.BoardId);
            _threadVisibility[thread.Id] = visible;
            return visible;
        }

        public bool CanSeeThread(int threadId)
        {
            if (!_threadVisibility.TryGetValue(threadId, out var visible))
            {
                visible = CanSeeThread(_threadRepository.GetById(threadId));
                _threadVisibility[threadId] = visible;
            }
            return visible;
        }

        public bool CanSeePostContent(Post post) =>
            (!post.IsDeleted || IsStaff) && CanSeeThread(post.ThreadId);
    }
}
