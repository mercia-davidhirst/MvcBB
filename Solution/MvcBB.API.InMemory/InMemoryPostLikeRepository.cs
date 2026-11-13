using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Post;

namespace MvcBB.API.InMemory
{
    public class InMemoryPostLikeRepository : IPostLikeRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryPostLikeRepository(InMemoryStore store)
        {
            _store = store;
        }

        public bool HasLiked(int postId, string userId) =>
            _store.PostLikes.Any(l => l.PostId == postId && l.UserId == userId);

        public int GetLikeCount(int postId) =>
            _store.PostLikes.Count(l => l.PostId == postId);

        public IReadOnlyDictionary<int, int> GetLikeCounts(IEnumerable<int> postIds)
        {
            var idSet = postIds.ToHashSet();
            return _store.PostLikes
                .Where(l => idSet.Contains(l.PostId))
                .GroupBy(l => l.PostId)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public ISet<int> GetLikedPostIds(IEnumerable<int> postIds, string userId)
        {
            var idSet = postIds.ToHashSet();
            return _store.PostLikes
                .Where(l => idSet.Contains(l.PostId) && l.UserId == userId)
                .Select(l => l.PostId)
                .ToHashSet();
        }

        public void AddLike(int postId, string userId)
        {
            if (HasLiked(postId, userId))
            {
                return;
            }

            var newId = _store.PostLikes.Count == 0 ? 1 : _store.PostLikes.Max(l => l.Id) + 1;
            _store.PostLikes.Add(new PostLike { Id = newId, PostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow });
        }

        public void RemoveLike(int postId, string userId)
        {
            var existing = _store.PostLikes.FirstOrDefault(l => l.PostId == postId && l.UserId == userId);
            if (existing != null)
            {
                _store.PostLikes.Remove(existing);
            }
        }
    }
}
