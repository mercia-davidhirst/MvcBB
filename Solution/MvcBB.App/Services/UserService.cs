using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Models.Member;
using MvcBB.Shared.Models.Authorization;

namespace MvcBB.App.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public UserService(IHttpClientFactory httpClientFactory, IAuthService authService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _authService = authService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task<IEnumerable<UserResponse>> GetUsersAsync()
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("api/users");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<UserResponse>>(content, _jsonOptions) ?? Array.Empty<UserResponse>();
        }

        // Includes email - requires the signed-in admin's own token, since the
        // API locks this endpoint to real Administrators.
        public async Task<IEnumerable<UserResponse>> GetUsersForAdminAsync()
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("api/users/admin");
            response.EnsureSuccessOrSessionExpired();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<UserResponse>>(content, _jsonOptions) ?? Array.Empty<UserResponse>();
        }

        public async Task<UserResponse?> GetUserByIdAsync(int userId)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"api/users/{userId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserResponse>(content, _jsonOptions);
        }

        public async Task<UserResponse?> GetUserByUsernameAsync(string username)
        {
            var response = await _httpClient.GetAsync($"api/users/by-username/{Uri.EscapeDataString(username)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserResponse>(content, _jsonOptions);
        }

        public async Task<MemberResponse> GetMembersAsync(MemberRequest request)
        {
            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"pageSize={request.PageSize}",
                $"sortDirection={request.SortDirection}"
            };

            if (!string.IsNullOrEmpty(request.Search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");
            }

            if (request.Role.HasValue)
            {
                queryParams.Add($"role={request.Role.Value}");
            }

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
            }

            var url = $"api/users/members?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MemberResponse>(content, _jsonOptions) 
                ?? throw new Exception("Failed to deserialize members response");
        }

        public async Task<UserResponse> GetUserProfileAsync()
        {
            // The API resolves "whose profile" from the caller's own NameIdentifier
            // claim rather than a route parameter, so this call needs the signed-in
            // user's own token - the generic mvc_client service token has no user
            // identity on it and would resolve to nobody.
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("api/users/profile");
            response.EnsureSuccessOrSessionExpired();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserResponse>(content, _jsonOptions) ?? throw new Exception("Failed to deserialize user profile");
        }

        public async Task<UserAuthResponse> RegisterAsync(UserRegistrationRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/users/register", content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserAuthResponse>(responseContent, _jsonOptions) ?? throw new Exception("Failed to deserialize auth response");
        }

        public async Task<UserAuthResponse> LoginAsync(UserLoginRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/users/login", content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserAuthResponse>(responseContent, _jsonOptions) ?? throw new Exception("Failed to deserialize auth response");
        }

        public async Task UpdateProfileAsync(int userId, UpdateUserRequest request)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/users/{userId}", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/users/{userId}/change-password", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<string> GetTokenAsync()
        {
            var credentials = new ApiCredentials
            {
                ClientId = _configuration["Api:ClientId"] ?? string.Empty,
                ClientSecret = _configuration["Api:ClientSecret"] ?? string.Empty
            };
            var response = await _authService.GetTokenAsync(credentials);
            return response.Token;
        }

        // Both of these require a real Administrator - the generic mvc_client
        // service token only carries the ApiClient role and gets a 403.
        private string GetCurrentUserToken()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
        }

        public async Task UpdateUserRoleAsync(int userId, UserRole newRole)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new UpdateUserRoleRequest
            {
                UserId = userId,
                NewRole = newRole
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"api/users/update-role", content);
            response.EnsureSuccessOrSessionExpired();
        }

        public async Task SetCustomTitleAsync(int userId, string? customTitle)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(new SetCustomTitleRequest { CustomTitle = customTitle });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/users/{userId}/custom-title", content);
            response.EnsureSuccessOrSessionExpired();
        }

        public async Task AnonymizeUserAsync(int userId)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync($"api/users/{userId}/anonymize", null);
            response.EnsureSuccessOrSessionExpired();
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/users/forgot-password", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/users/reset-password", content);
            response.EnsureSuccessStatusCode();
        }
    }
} 