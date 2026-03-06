using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.Shared.Interfaces
{
    public interface IForumThreadRepository
    {
        IReadOnlyList<ForumThread> GetAll();
        IReadOnlyList<ForumThread> GetByBoardId(int boardId);
        ForumThread? GetById(int id);
        ForumThread Add(ForumThread thread);
        void Update(ForumThread thread);
        void Remove(ForumThread thread);
    }
}
