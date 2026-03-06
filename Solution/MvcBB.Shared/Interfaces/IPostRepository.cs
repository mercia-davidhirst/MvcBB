using MvcBB.Shared.Models.Post;

namespace MvcBB.Shared.Interfaces
{
    public interface IPostRepository
    {
        IReadOnlyList<Post> GetAll();
        IReadOnlyList<Post> GetByThreadId(int threadId);
        Post? GetById(int id);
        Post Add(Post post);
        void Update(Post post);
        void Remove(Post post);
    }
}
