using MvcBB.Shared.Models.Settings;

namespace MvcBB.Shared.Services
{
    /// <summary>
    /// Checks a candidate password against the admin-configured PasswordPolicy.
    /// Shared (not API-only) so both the API and anything else that needs to
    /// describe the policy to a user can use the exact same rules/wording.
    /// </summary>
    public static class PasswordPolicyValidator
    {
        public static bool TryValidate(string password, PasswordPolicy policy, out string? errorMessage)
        {
            var problems = new List<string>();

            if (password.Length < policy.MinimumLength)
            {
                problems.Add($"at least {policy.MinimumLength} characters");
            }
            if (policy.RequireUppercase && !password.Any(char.IsUpper))
            {
                problems.Add("an uppercase letter");
            }
            if (policy.RequireLowercase && !password.Any(char.IsLower))
            {
                problems.Add("a lowercase letter");
            }
            if (policy.RequireDigit && !password.Any(char.IsDigit))
            {
                problems.Add("a digit");
            }
            if (policy.RequireSpecialCharacter && !password.Any(c => !char.IsLetterOrDigit(c)))
            {
                problems.Add("a special character");
            }

            if (problems.Count == 0)
            {
                errorMessage = null;
                return true;
            }

            errorMessage = "Password must contain " + string.Join(", ", problems) + ".";
            return false;
        }

        /// <summary>Human-readable summary of the policy, for hint text on password forms.</summary>
        public static string Describe(PasswordPolicy policy)
        {
            var requirements = new List<string> { $"at least {policy.MinimumLength} characters" };

            if (policy.RequireUppercase) requirements.Add("an uppercase letter");
            if (policy.RequireLowercase) requirements.Add("a lowercase letter");
            if (policy.RequireDigit) requirements.Add("a digit");
            if (policy.RequireSpecialCharacter) requirements.Add("a special character");

            return "Password must contain " + string.Join(", ", requirements) + ".";
        }
    }
}
