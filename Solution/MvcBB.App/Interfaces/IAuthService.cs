using MvcBB.Shared.Models.Authorization;

namespace MvcBB.App.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> GetTokenAsync(ApiCredentials credentials);
    }
} 