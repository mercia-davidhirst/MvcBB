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

        /// <summary>
        /// For each of the given thread ids: how many distinct users posted,
        /// and every post id in that thread - one query instead of one
        /// GetByThreadId call per thread. A thread with no posts is simply
        /// absent from the result.
        /// </summary>
        IReadOnlyDictionary<int, ThreadPostSummary> GetThreadSummaries(IEnumerable<int> threadIds);

        /// <summary>Multiple posts by id in one query - e.g. batch-resolving quoted posts on a page.</summary>
        IReadOnlyDictionary<int, Post> GetByIds(IEnumerable<int> ids);
    }
}
