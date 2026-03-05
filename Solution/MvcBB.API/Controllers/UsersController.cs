using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Models.Member;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.Common;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private static readonly List<User> _users = new();
        private readonly IConfiguration _configuration;
        private static readonly List<ForumThread> _threads = new();
        private static readonly List<Post> _posts = new();
        private static bool _isInitialized = false;

        public UsersController(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeAdminUser();
        }

        private void InitializeAdminUser()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Create admin user if no users exist
            if (!_users.Any())
            {
                var adminUser = new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = HashPassword("Admin123!"),
                    CreatedAt = DateTime.UtcNow,
                    Role = UserRole.Administrator
                };
                _users.Add(adminUser);
            }
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? 
                throw new InvalidOperationException("JWT secret key is not configured"));
            var tokenExpirationHours = _configuration.GetValue<int>("Jwt:TokenExpirationHours");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(tokenExpirationHours),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpPost("register")]
        public ActionResult<UserAuthResponse> Register(UserRegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_users.Any(u => u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { message = "Username is already taken" });
            }

            if (_users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { message = "Email is already registered" });
            }

            var user = new User
            {
                Id = _users.Count + 1,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                Role = UserRole.User // New users are always regular users
            };

            _users.Add(user);

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(1);

            return new UserAuthResponse
            {
                Token = token,
                User = new UserResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Role = user.Role
                },
                ExpiresAt = expiresAt
            };
        }

        [HttpPost("login")]
        public ActionResult<UserAuthResponse> Login(UserLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _users.FirstOrDefault(u => 
                u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            user.LastLoginAt = DateTime.UtcNow;

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(1);

            return new UserAuthResponse
            {
                Token = token,
                User = new UserResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Role = user.Role
                },
                ExpiresAt = expiresAt
            };
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserResponse>> GetUsers()
        {
            return Ok(_users.Select(u => MapToUserResponse(u)));
        }

        [HttpGet("members")]
        public ActionResult<MemberResponse> GetMembers([FromQuery] MemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var query = _users.AsQueryable();

            // Apply role filter if specified
            if (request.Role.HasValue)
            {
                query = query.Where(u => u.Role == request.Role.Value);
            }

            // Apply search filter if specified
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(search));
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "username" => request.SortDirection == SortDirection.Descending 
                    ? query.OrderByDescending(u => u.Username)
                    : query.OrderBy(u => u.Username),
                "joined" => request.SortDirection == SortDirection.Descending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt),
                "posts" => request.SortDirection == SortDirection.Descending
                    ? query.OrderByDescending(u => _posts.Count(p => p.CreatedByUserId == u.Username))
                    : query.OrderBy(u => _posts.Count(p => p.CreatedByUserId == u.Username)),
                "threads" => request.SortDirection == SortDirection.Descending
                    ? query.OrderByDescending(u => _threads.Count(t => t.CreatedByUserId == u.Username))
                    : query.OrderBy(u => _threads.Count(t => t.CreatedByUserId == u.Username)),
                _ => query.OrderBy(u => u.Username)
            };

            // Calculate pagination
            var totalMembers = query.Count();
            var pageSize = Math.Max(1, request.PageSize);
            var page = Math.Max(1, request.Page);
            var totalPages = (int)Math.Ceiling(totalMembers / (double)pageSize);
            var skip = (page - 1) * pageSize;

            // Get paginated results (materialize then map so MapToUserResponse is not in expression tree)
            var memberUsers = query.Skip(skip).Take(pageSize).ToList();
            var members = memberUsers.Select(u => MapToUserResponse(u, false)).ToList();

            return Ok(new MemberResponse
            {
                Members = members,
                TotalMembers = totalMembers,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        [Authorize]
        [HttpGet("profile")]
        public ActionResult<UserResponse> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = _users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return MapToUserResponse(user, includeEmail: true);
        }

        [Authorize]
        [HttpPut("{userId}")]
        public ActionResult UpdateProfile(int userId, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Client credentials (ApiClient) can update any userId (trusted app); user JWT must match userId
            if (!User.IsInRole("ApiClient"))
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId != currentUserId)
                {
                    return Forbid();
                }
            }

            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!user.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) &&
                _users.Any(u => u.Id != userId && u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { message = "Username is already taken" });
            }

            if (!user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase) &&
                _users.Any(u => u.Id != userId && u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { message = "Email is already registered" });
            }

            user.Username = request.Username;
            user.Email = request.Email;
            user.Signature = request.Signature;
            user.Bio = request.Bio;
            user.AvatarUrl = request.AvatarUrl;
            user.ShowEmail = request.ShowEmail;

            return NoContent();
        }

        [Authorize]
        [HttpPost("{userId}/change-password")]
        public ActionResult ChangePassword(int userId, [FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Client credentials (ApiClient) can change password for any userId (trusted app); user JWT must match userId
            if (!User.IsInRole("ApiClient"))
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId != currentUserId)
                {
                    return Forbid();
                }
            }

            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            return NoContent();
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("update-role")]
        public ActionResult UpdateUserRole([FromBody] UpdateUserRoleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _users.FirstOrDefault(u => u.Id == request.UserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Prevent changing own role
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                return BadRequest(new { message = "Cannot change your own role" });
            }

            user.Role = request.NewRole;
            return Ok(new { message = "User role updated successfully" });
        }

        private UserResponse MapToUserResponse(User u, bool includeEmail = false)
        {
            return new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = includeEmail ? u.Email : string.Empty,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                ThreadCount = _threads.Count(t => t.CreatedByUserId == u.Username),
                PostCount = _posts.Count(p => p.CreatedByUserId == u.Username),
                Role = u.Role,
                Signature = u.Signature,
                Bio = u.Bio,
                AvatarUrl = u.AvatarUrl,
                ShowEmail = u.ShowEmail
            };
        }
    }
} 