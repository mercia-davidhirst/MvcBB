using MvcBB.Shared.Models.Settings;

namespace MvcBB.Shared.Interfaces
{
    /// <summary>
    /// Site-wide password complexity requirements - a single configurable
    /// row, seeded once by the installer's seed data, never inserted/
    /// deleted at runtime.
    /// </summary>
    public interface IPasswordPolicyRepository
    {
        PasswordPolicy Get();
        void Update(PasswordPolicy policy);
    }
}
