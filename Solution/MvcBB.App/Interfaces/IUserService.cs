using MvcBB.Shared.Models.User;
using MvcBB.Shared.Models.Member;

namespace MvcBB.App.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponse>> GetUsersAsync();
        Task<IEnumerable<UserResponse>> GetUsersForAdminAsync();

        /// <summary>Total member count and how many have logged in within the last activeSinceDays days - for the homepage statistics panel.</summary>
        Task<UserStatsResponse> GetUserStatsAsync(int activeSinceDays = 7);
        Task<UserResponse> GetUserProfileAsync();
        Task<UserResponse?> GetUserByIdAsync(int userId);
        Task<UserResponse?> GetUserByUsernameAsync(string username);
        Task<UserAuthResponse> RegisterAsync(UserRegistrationRequest request);
        Task<UserAuthResponse> LoginAsync(UserLoginRequest request);
        Task<MemberResponse> GetMembersAsync(MemberRequest request);
        Task UpdateProfileAsync(int userId, UpdateUserRequest request);
        Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task UpdateUserRoleAsync(int userId, UserRole newRole);

        /// <summary>Admin-only. Null or empty clears the override and falls back to the computed rank.</summary>
        Task SetCustomTitleAsync(int userId, string? customTitle);
        Task AnonymizeUserAsync(int userId);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);

        /// <summary>Admin-only. Forces the user to set a new password on their next login.</summary>
        Task RequirePasswordChangeAsync(int userId);

        /// <summary>Admin-only. Forces every non-deleted user to set a new password on their next login - e.g. after tightening the password policy. Returns how many users were affected.</summary>
        Task<int> RequirePasswordChangeForAllAsync();

        /// <summary>Admin-only. Changes a user's email without requiring their current password.</summary>
        Task UpdateUserEmailAsync(int userId, string email);

        /// <summary>Admin-only. Null or a past date unlocks the account; a future date locks it until then.</summary>
        Task LockUserAsync(int userId, DateTime? lockedUntil);

        /// <summary>Admin-only. Creates a user directly (no CAPTCHA, no auto sign-in, not subject to the registration-enabled toggle).</summary>
        Task<UserResponse> AdminCreateUserAsync(AdminCreateUserRequest request);
    }
} 