using MvcBB.Shared.Models.User;

namespace MvcBB.Shared.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        PasswordResetToken Add(PasswordResetToken token);
        PasswordResetToken? GetByTokenHash(string tokenHash);
        void MarkUsed(int id);
    }
}
