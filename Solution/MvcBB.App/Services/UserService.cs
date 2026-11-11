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
            await EnsureSuccessOrThrowAsync(response);

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserAuthResponse>(responseContent, _jsonOptions) ?? throw new Exception("Failed to deserialize auth response");
        }

        public async Task<UserAuthResponse> LoginAsync(UserLoginRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/users/login", content);

            // 403 means the account is locked - surface that specific message rather
            // than the generic "Invalid login attempt" used for a plain 401 (wrong
            // username/password), since a locked-out user needs to know why.
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                try
                {
                    using var document = JsonDocument.Parse(errorBody);
                    if (document.RootElement.TryGetProperty("message", out var messageProp))
                    {
                        throw new ServiceException(messageProp.GetString() ?? "This account is locked.");
                    }
                }
                catch (JsonException)
                {
                    // Fall through to the generic failure below.
                }
            }

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
            await EnsureSuccessOrThrowAsync(response);
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
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task RequirePasswordChangeAsync(int userId)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync($"api/users/{userId}/require-password-change", null);
            response.EnsureSuccessOrSessionExpired();
        }

        public async Task<int> RequirePasswordChangeForAllAsync()
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync("api/users/require-password-change-all", null);
            response.EnsureSuccessOrSessionExpired();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            return document.RootElement.TryGetProperty("affectedCount", out var countProp) ? countProp.GetInt32() : 0;
        }

        public async Task UpdateUserEmailAsync(int userId, string email)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(new AdminSetEmailRequest { Email = email });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/users/{userId}/email", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task LockUserAsync(int userId, DateTime? lockedUntil)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(new LockUserRequest { LockedUntil = lockedUntil });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/users/{userId}/lock", content);
            await EnsureSuccessOrThrowAsync(response);
        }

        public async Task<UserResponse> AdminCreateUserAsync(AdminCreateUserRequest request)
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/users/admin-create", content);
            await EnsureSuccessOrThrowAsync(response);

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserResponse>(responseContent, _jsonOptions) ?? throw new Exception("Failed to deserialize user response");
        }

        // A 400 from these endpoints carries a specific validation message (e.g. a
        // duplicate username/email) in its ValidationProblemDetails body -
        // EnsureSuccessStatusCode would discard that and leave the caller with a
        // generic failure, so extract it here.
        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new SessionExpiredException();
            }

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var document = JsonDocument.Parse(body);
                var errorsElement = document.RootElement.TryGetProperty("errors", out var errors)
                    ? errors
                    : document.RootElement;

                if (errorsElement.ValueKind == JsonValueKind.Object)
                {
                    var messages = errorsElement.EnumerateObject()
                        .Where(prop => prop.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(prop => prop.Value.EnumerateArray().Select(v => v.GetString()))
                        .Where(m => !string.IsNullOrEmpty(m));
                    var message = string.Join(" ", messages);
                    if (!string.IsNullOrEmpty(message))
                    {
                        throw new ServiceException(message);
                    }
                }
                else if (document.RootElement.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                {
                    var message = messageProp.GetString();
                    if (!string.IsNullOrEmpty(message))
                    {
                        throw new ServiceException(message);
                    }
                }
            }
            catch (JsonException)
            {
                // Body wasn't a recognized error payload - fall through to the generic exception below.
            }

            response.EnsureSuccessStatusCode();
        }
    }
} 