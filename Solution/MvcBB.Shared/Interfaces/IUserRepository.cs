using MvcBB.Shared.Models.User;

namespace MvcBB.Shared.Interfaces
{
    public interface IUserRepository
    {
        IReadOnlyList<User> GetAll();
        User? GetById(int id);
        User? GetByUsername(string username);
        User? GetByEmail(string email);
        User Add(User user);
        void Update(User user);
        bool ExistsWithUsername(string username, int? excludeUserId = null);
        bool ExistsWithEmail(string email, int? excludeUserId = null);
        int CountPostsByUsername(string username);
        int CountThreadsByUsername(string username);

        /// <summary>Multiple users by id in one query - e.g. batch-resolving avatar/rank/custom title for a page of posts.</summary>
        IReadOnlyDictionary<int, User> GetByIds(IEnumerable<int> ids);

        /// <summary>Username for each of the given ids, in one query - for when only the name is needed.</summary>
        IReadOnlyDictionary<int, string> GetUsernamesByIds(IEnumerable<int> ids);

        /// <summary>Post counts for several usernames at once, in one query.</summary>
        IReadOnlyDictionary<string, int> GetPostCountsByUsernames(IEnumerable<string> usernames);

        /// <summary>Thread counts for several usernames at once, in one query.</summary>
        IReadOnlyDictionary<string, int> GetThreadCountsByUsernames(IEnumerable<string> usernames);

        /// <summary>
        /// Users whose username or email contains the query (case-
        /// insensitive), newest-first and paginated entirely in the data
        /// layer. TotalCount is the full filtered count, not just the page
        /// returned.
        /// </summary>
        (IReadOnlyList<User> Items, int TotalCount) Search(string query, int skip, int take);

        /// <summary>
        /// A single page of users, optionally filtered by role and/or a
        /// username search substring (case-insensitive), sorted by
        /// username/joined date/post count/thread count - entirely in the
        /// data layer (including the posts/threads sort, via a correlated
        /// count), instead of loading every user in the table first to
        /// filter/sort/paginate in memory. TotalCount is the full filtered
        /// count, not just the page returned. sortBy recognizes "joined",
        /// "posts", "threads" and "username" (case-insensitive); anything
        /// else falls back to username.
        /// </summary>
        (IReadOnlyList<User> Items, int TotalCount) GetMembersPage(UserRole? role, string? search, string? sortBy, bool sortDescending, int skip, int take);

        /// <summary>
        /// Total user count and how many have logged in since activeSince -
        /// both computed as aggregate COUNTs in the data layer (the
        /// homepage statistics panel's need), instead of loading every user
        /// to call Count()/Count(predicate) in memory.
        /// </summary>
        (int TotalMembers, int ActiveMembers) GetStats(DateTime activeSince);
    }
}
