using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.InMemory
{
    public class InMemoryPasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryPasswordResetTokenRepository(InMemoryStore store)
        {
            _store = store;
        }

        public PasswordResetToken Add(PasswordResetToken token)
        {
            if (token.Id == 0)
                token.Id = _store.PasswordResetTokens.Count == 0 ? 1 : _store.PasswordResetTokens.Max(t => t.Id) + 1;
            _store.PasswordResetTokens.Add(token);
            return token;
        }

        public PasswordResetToken? GetByTokenHash(string tokenHash) =>
            _store.PasswordResetTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        public void MarkUsed(int id)
        {
            var token = _store.PasswordResetTokens.FirstOrDefault(t => t.Id == id);
            if (token != null)
                token.UsedAt = DateTime.UtcNow;
        }
    }
}
