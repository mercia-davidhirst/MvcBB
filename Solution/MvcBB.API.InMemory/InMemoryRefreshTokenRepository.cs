using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.InMemory
{
    public class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly InMemoryStore _store;
        private readonly object _lock = new();

        public InMemoryRefreshTokenRepository(InMemoryStore store)
        {
            _store = store;
        }

        public RefreshToken Add(RefreshToken token)
        {
            lock (_lock)
            {
                if (token.Id == 0)
                    token.Id = _store.RefreshTokens.Count == 0 ? 1 : _store.RefreshTokens.Max(t => t.Id) + 1;
                _store.RefreshTokens.Add(token);
                return token;
            }
        }

        public RefreshToken? GetByTokenHash(string tokenHash)
        {
            lock (_lock)
            {
                return _store.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
            }
        }

        public bool MarkUsed(int id, DateTime usedAt)
        {
            lock (_lock)
            {
                var token = _store.RefreshTokens.FirstOrDefault(t => t.Id == id);
                if (token == null || token.UsedAt != null || token.RevokedAt != null)
                    return false;
                token.UsedAt = usedAt;
                return true;
            }
        }

        public void RevokeFamily(Guid familyId)
        {
            lock (_lock)
            {
                foreach (var token in _store.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedAt == null))
                    token.RevokedAt = DateTime.UtcNow;
            }
        }

        public void RevokeAllForUser(int userId)
        {
            lock (_lock)
            {
                foreach (var token in _store.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null))
                    token.RevokedAt = DateTime.UtcNow;
            }
        }

        public int Prune(DateTime now, DateTime spentBefore)
        {
            lock (_lock)
            {
                return _store.RefreshTokens.RemoveAll(t =>
                    t.ExpiresAt < now || t.UsedAt < spentBefore || t.RevokedAt < spentBefore);
            }
        }
    }
}
