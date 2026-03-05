using MvcBB.Shared.Models.User;
using MvcBB.Shared.Models.Member;

namespace MvcBB.App.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponse>> GetUsersAsync();
        Task<UserResponse> GetUserProfileAsync();
        Task<UserResponse?> GetUserByIdAsync(int userId);
        Task<UserAuthResponse> RegisterAsync(UserRegistrationRequest request);
        Task<UserAuthResponse> LoginAsync(UserLoginRequest request);
        Task<MemberResponse> GetMembersAsync(MemberRequest request);
        Task UpdateProfileAsync(int userId, UpdateUserRequest request);
        Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task UpdateUserRoleAsync(int userId, UserRole newRole);
    }
} 