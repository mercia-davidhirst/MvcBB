using MvcBB.Shared.Models.User;

namespace MvcBB.Shared.Interfaces
{
    public interface IRefreshTokenRepository
    {
        RefreshToken Add(RefreshToken token);
        RefreshToken? GetByTokenHash(string tokenHash);

        /// <summary>
        /// Marks the token used, only if it is still unused and not revoked.
        /// Returns false if another request got there first, so two requests
        /// can never both rotate the same token.
        /// </summary>
        bool MarkUsed(int id, DateTime usedAt);

        /// <summary>Revokes every live token in one sign-in (family).</summary>
        void RevokeFamily(Guid familyId);

        /// <summary>Revokes every live token the member has, on every device.</summary>
        void RevokeAllForUser(int userId);

        /// <summary>
        /// Deletes tokens that have expired, and used or revoked tokens older
        /// than the cutoff. Returns how many went.
        /// </summary>
        int Prune(DateTime now, DateTime spentBefore);
    }
}
