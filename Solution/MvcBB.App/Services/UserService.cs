using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public UserService(IHttpClientFactory httpClientFactory, IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _authService = authService;
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
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("api/users/profile");
            response.EnsureSuccessStatusCode();
            
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
                ClientId = "mvc_client",
                ClientSecret = "your_secure_secret_here"
            };
            var response = await _authService.GetTokenAsync(credentials);
            return response.Token;
        }

        public async Task UpdateUserRoleAsync(int userId, UserRole newRole)
        {
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new UpdateUserRoleRequest
            {
                UserId = userId,
                NewRole = newRole
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/users/update-role", content);
            response.EnsureSuccessStatusCode();
        }
    }
} 