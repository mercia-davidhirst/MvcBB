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

        public int CountPostsByUsername(string username) =>
            _store.Posts.Count(p => p.CreatedByUserId == username);

        public int CountThreadsByUsername(string username) =>
            _store.Threads.Count(t => t.CreatedByUserId == username);
    }
}
