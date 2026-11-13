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
    }
}
