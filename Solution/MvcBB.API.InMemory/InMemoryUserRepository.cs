using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.InMemory
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryUserRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<User> GetAll() => _store.Users.ToList();

        public User? GetById(int id) => _store.Users.FirstOrDefault(u => u.Id == id);

        public User? GetByUsername(string username) =>
            _store.Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        public User? GetByEmail(string email) =>
            _store.Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        public User Add(User user)
        {
            if (user.Id == 0)
                user.Id = _store.Users.Count == 0 ? 1 : _store.Users.Max(u => u.Id) + 1;
            _store.Users.Add(user);
            return user;
        }

        public void Update(User user)
        {
            var index = _store.Users.FindIndex(u => u.Id == user.Id);
            if (index >= 0)
                _store.Users[index] = user;
        }

        public bool ExistsWithUsername(string username, int? excludeUserId = null) =>
            _store.Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Id != excludeUserId);

        public bool ExistsWithEmail(string email, int? excludeUserId = null) =>
            _store.Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.Id != excludeUserId);

        public int CountPostsByUsername(string username)
        {
            var user = GetByUsername(username);
            if (user == null)
                return 0;

            var userId = user.Id.ToString();
            return _store.Posts.Count(p => p.CreatedByUserId == userId && !p.IsDeleted);
        }

        public int CountThreadsByUsername(string username)
        {
            var user = GetByUsername(username);
            if (user == null)
                return 0;

            var userId = user.Id.ToString();
            return _store.Threads.Count(t => t.CreatedByUserId == userId);
        }

        public IReadOnlyDictionary<int, User> GetByIds(IEnumerable<int> ids)
        {
            var idSet = ids.ToHashSet();
            return _store.Users.Where(u => idSet.Contains(u.Id)).ToDictionary(u => u.Id);
        }

        public IReadOnlyDictionary<int, string> GetUsernamesByIds(IEnumerable<int> ids)
        {
            var idSet = ids.ToHashSet();
            return _store.Users.Where(u => idSet.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Username);
        }

        public IReadOnlyDictionary<string, int> GetPostCountsByUsernames(IEnumerable<string> usernames)
        {
            var nameSet = usernames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return _store.Users
                .Where(u => nameSet.Contains(u.Username))
                .ToDictionary(
                    u => u.Username,
                    u => _store.Posts.Count(p => p.CreatedByUserId == u.Id.ToString() && !p.IsDeleted),
                    StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, int> GetThreadCountsByUsernames(IEnumerable<string> usernames)
        {
            var nameSet = usernames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return _store.Users
                .Where(u => nameSet.Contains(u.Username))
                .ToDictionary(
                    u => u.Username,
                    u => _store.Threads.Count(t => t.CreatedByUserId == u.Id.ToString()),
                    StringComparer.OrdinalIgnoreCase);
        }

        public (IReadOnlyList<User> Items, int TotalCount) Search(string query, int skip, int take)
        {
            var q = query.ToLowerInvariant();
            var filtered = _store.Users
                .Where(u => u.Username.ToLowerInvariant().Contains(q) || u.Email.ToLowerInvariant().Contains(q))
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            return (filtered.Skip(skip).Take(take).ToList(), filtered.Count);
        }
    }
}
