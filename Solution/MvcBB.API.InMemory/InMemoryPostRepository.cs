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

        public IReadOnlyList<Post> GetAll() => _store.Posts.Select(Hydrate).ToList();

        public IReadOnlyList<Post> GetByThreadId(int threadId) =>
            _store.Posts.Where(p => p.ThreadId == threadId).OrderBy(p => p.CreatedAt).Select(Hydrate).ToList();

        public Post? GetById(int id)
        {
            var post = _store.Posts.FirstOrDefault(p => p.Id == id);
            return post == null ? null : Hydrate(post);
        }

        public Post Add(Post post)
        {
            if (post.Id == 0)
                post.Id = _store.Posts.Count == 0 ? 1 : _store.Posts.Max(p => p.Id) + 1;
            _store.Posts.Add(post);
            return Hydrate(post);
        }

        public void Update(Post post)
        {
            var index = _store.Posts.FindIndex(p => p.Id == post.Id);
            if (index >= 0)
                _store.Posts[index] = post;
        }

        public IReadOnlyDictionary<int, ThreadPostSummary> GetThreadSummaries(IEnumerable<int> threadIds)
        {
            var idSet = threadIds.ToHashSet();
            return _store.Posts
                .Where(p => idSet.Contains(p.ThreadId))
                .GroupBy(p => p.ThreadId)
                .ToDictionary(g => g.Key, g => new ThreadPostSummary
                {
                    ParticipantCount = g.Select(p => p.CreatedByUserId).Distinct().Count(),
                    PostIds = g.Select(p => p.Id).ToList()
                });
        }

        public IReadOnlyDictionary<int, Post> GetByIds(IEnumerable<int> ids)
        {
            var idSet = ids.ToHashSet();
            return _store.Posts.Where(p => idSet.Contains(p.Id)).Select(Hydrate).ToDictionary(p => p.Id);
        }

        public IReadOnlyList<Post> GetByUserId(int userId, int take)
        {
            var idString = userId.ToString();
            return _store.Posts
                .Where(p => !p.IsDeleted && p.CreatedByUserId == idString)
                .Select(Hydrate)
                .OrderByDescending(p => p.CreatedAt)
                .Take(take)
                .ToList();
        }

        public (IReadOnlyList<Post> Items, int TotalCount) Search(string query, int? boardId, bool includePrivateBoards, int skip, int take)
        {
            var boardIdByThreadId = _store.Threads.ToDictionary(t => t.Id, t => t.BoardId);
            var boardsById = _store.Boards.ToDictionary(b => b.Id);
            var q = query.ToLowerInvariant();

            var filtered = _store.Posts
                .Select(Hydrate)
                .Where(p => p.Content.ToLowerInvariant().Contains(q) || p.CreatedByUsername.ToLowerInvariant().Contains(q))
                .Where(p =>
                {
                    var board = boardIdByThreadId.TryGetValue(p.ThreadId, out var bId) && boardsById.TryGetValue(bId, out var b) ? b : null;
                    if (boardId.HasValue && board?.Id != boardId.Value)
                        return false;
                    return includePrivateBoards || board == null || !board.IsPrivate;
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return (filtered.Skip(skip).Take(take).ToList(), filtered.Count);
        }

        // Populates the denormalized presentation fields (ThreadTitle,
        // CreatedByUsername/UpdatedByUsername/DeletedByUsername, UserAvatar,
        // UserSignature, UserPostCount, UserJoinedAt, UserRole) from the
        // current Thread/User records, matching what the SQL/PostgreSQL
        // repositories get for free from vw_PostDetails/vw_post_details.
        private Post Hydrate(Post post)
        {
            post.ThreadTitle = _store.Threads.FirstOrDefault(t => t.Id == post.ThreadId)?.Title ?? string.Empty;

            if (int.TryParse(post.CreatedByUserId, out var creatorId))
            {
                var creator = _store.Users.FirstOrDefault(u => u.Id == creatorId);
                post.CreatedByUsername = creator?.Username ?? string.Empty;
                post.UserAvatar = creator?.AvatarUrl;
                post.UserSignature = creator?.Signature;
                post.UserJoinedAt = creator?.CreatedAt ?? default;
                post.UserRole = creator?.Role.ToString();
            }

            post.UserPostCount = _store.Posts.Count(p => p.CreatedByUserId == post.CreatedByUserId && !p.IsDeleted);

            post.UpdatedByUsername = int.TryParse(post.UpdatedByUserId, out var updaterId)
                ? _store.Users.FirstOrDefault(u => u.Id == updaterId)?.Username
                : null;

            post.DeletedByUsername = int.TryParse(post.DeletedByUserId, out var deleterId)
                ? _store.Users.FirstOrDefault(u => u.Id == deleterId)?.Username
                : null;

            return post;
        }
    }
}
