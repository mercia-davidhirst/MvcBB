using MvcBB.Shared.Models.Settings;

namespace MvcBB.API.Services
{
    public interface IPasswordPolicyService
    {
        PasswordPolicy GetPasswordPolicy();
        void UpdatePasswordPolicy(PasswordPolicy policy);
    }

    public class PasswordPolicyService : IPasswordPolicyService
    {
        private static PasswordPolicy _policy = new();
        private readonly IConfiguration _configuration;
        private static bool _initialized;

        public PasswordPolicyService(IConfiguration configuration)
        {
            _configuration = configuration;
            if (!_initialized)
            {
                _policy.MinimumLength = _configuration.GetValue("PasswordPolicy:MinimumLength", 8);
                _policy.RequireUppercase = _configuration.GetValue("PasswordPolicy:RequireUppercase", false);
                _policy.RequireLowercase = _configuration.GetValue("PasswordPolicy:RequireLowercase", false);
                _policy.RequireDigit = _configuration.GetValue("PasswordPolicy:RequireDigit", false);
                _policy.RequireSpecialCharacter = _configuration.GetValue("PasswordPolicy:RequireSpecialCharacter", false);
                _initialized = true;
            }
        }

        public PasswordPolicy GetPasswordPolicy()
        {
            return new PasswordPolicy
            {
                MinimumLength = _policy.MinimumLength,
                RequireUppercase = _policy.RequireUppercase,
                RequireLowercase = _policy.RequireLowercase,
                RequireDigit = _policy.RequireDigit,
                RequireSpecialCharacter = _policy.RequireSpecialCharacter
            };
        }

        public void UpdatePasswordPolicy(PasswordPolicy policy)
        {
            _policy.MinimumLength = Math.Clamp(policy.MinimumLength, 4, 100);
            _policy.RequireUppercase = policy.RequireUppercase;
            _policy.RequireLowercase = policy.RequireLowercase;
            _policy.RequireDigit = policy.RequireDigit;
            _policy.RequireSpecialCharacter = policy.RequireSpecialCharacter;
        }
    }
}
