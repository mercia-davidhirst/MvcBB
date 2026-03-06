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
    }
}
