using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.Services
{
    public interface IPasswordPolicyService
    {
        PasswordPolicy GetPasswordPolicy();
        void UpdatePasswordPolicy(PasswordPolicy policy);
    }

    /// <summary>
    /// Thin wrapper over IPasswordPolicyRepository - previously held its own
    /// state in a static field (seeded once from appsettings.json), which
    /// meant admin changes never survived an app restart and never applied
    /// to the other side of a multi-instance deployment. Now every call
    /// reads/writes the single PasswordPolicy row through the data layer.
    /// </summary>
    public class PasswordPolicyService : IPasswordPolicyService
    {
        private readonly IPasswordPolicyRepository _repository;

        public PasswordPolicyService(IPasswordPolicyRepository repository)
        {
            _repository = repository;
        }

        public PasswordPolicy GetPasswordPolicy() => _repository.Get();

        public void UpdatePasswordPolicy(PasswordPolicy policy)
        {
            policy.MinimumLength = Math.Clamp(policy.MinimumLength, 4, 100);
            _repository.Update(policy);
        }
    }
}
