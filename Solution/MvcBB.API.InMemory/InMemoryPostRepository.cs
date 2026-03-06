using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Post;

namespace MvcBB.API.InMemory
{
    public class InMemoryPostRepository : IPostRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryPostRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<Post> GetAll() => _store.Posts.ToList();

        public IReadOnlyList<Post> GetByThreadId(int threadId) =>
            _store.Posts.Where(p => p.ThreadId == threadId).OrderBy(p => p.CreatedAt).ToList();

        public Post? GetById(int id) => _store.Posts.FirstOrDefault(p => p.Id == id);

        public Post Add(Post post)
        {
            if (post.Id == 0)
                post.Id = _store.Posts.Count == 0 ? 1 : _store.Posts.Max(p => p.Id) + 1;
            _store.Posts.Add(post);
            return post;
        }

        public void Update(Post post)
        {
            var index = _store.Posts.FindIndex(p => p.Id == post.Id);
            if (index >= 0)
                _store.Posts[index] = post;
        }

        public void Remove(Post post) => _store.Posts.Remove(post);
    }
}
