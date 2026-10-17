using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Models.Member;
using MvcBB.Shared.Models.Common;
using MvcBB.Shared.Interfaces;
using MvcBB.API.Services;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IEmailSender _emailSender;

        public UsersController(
            IConfiguration configuration,
            IUserRepository userRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IEmailSender emailSender)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailSender = emailSender;
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        private static bool VerifyPassword(string password, string hashedPassword)
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

            if (_userRepository.ExistsWithUsername(request.Username))
            {
                return BadRequest(new { message = "Username is already taken" });
            }

            if (_userRepository.ExistsWithEmail(request.Email))
            {
                return BadRequest(new { message = "Email is already registered" });
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                Role = UserRole.User
            };

            user = _userRepository.Add(user);

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

            var user = _userRepository.GetByUsername(request.Username);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            user.LastLoginAt = DateTime.UtcNow;
            _userRepository.Update(user);

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

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _userRepository.GetByEmail(request.Email);

            // Always respond the same way regardless of whether the email
            // matched an account, to avoid leaking which emails are registered.
            if (user != null && user.Role != UserRole.Deleted)
            {
                var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                var expirationMinutes = _configuration.GetValue<int?>("PasswordReset:TokenExpirationMinutes") ?? 60;

                _passwordResetTokenRepository.Add(new PasswordResetToken
                {
                    UserId = user.Id,
                    TokenHash = HashToken(rawToken),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                });

                var appBaseUrl = _configuration["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
                var resetLink = $"{appBaseUrl}/Account/ResetPassword?token={rawToken}";

                await _emailSender.SendAsync(
                    user.Email,
                    "Reset your MvcBB password",
                    $"Click the link below to reset your password. This link expires in {expirationMinutes} minutes.\n\n{resetLink}\n\nIf you didn't request this, you can ignore this email.");
            }

            return Ok(new { message = "If that email is registered, a password reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public ActionResult ResetPassword(ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = _passwordResetTokenRepository.GetByTokenHash(HashToken(request.Token));
            if (token == null || token.UsedAt != null || token.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "This reset link is invalid or has expired." });
            }

            var user = _userRepository.GetById(token.UserId);
            if (user == null || user.Role == UserRole.Deleted)
            {
                return BadRequest(new { message = "This reset link is invalid or has expired." });
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            _userRepository.Update(user);
            _passwordResetTokenRepository.MarkUsed(token.Id);

            return Ok(new { message = "Password reset successfully." });
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserResponse>> GetUsers()
        {
            var users = _userRepository.GetAll();
            return Ok(users.Select(u => MapToUserResponse(u)));
        }

        [HttpGet("members")]
        public ActionResult<MemberResponse> GetMembers([FromQuery] MemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var query = _userRepository.GetAll().AsEnumerable();

            if (request.Role.HasValue)
            {
                query = query.Where(u => u.Role == request.Role.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(search));
            }

            query = request.SortBy?.ToLower() switch
            {
                "username" => request.SortDirection == SortDirection.Descending
                    ? query.OrderByDescending(u => u.Username)
                    : query.OrderBy(u => u.Username),
                "joined" => request.SortDirection == SortDirection.Descending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt),
                "posts" => request.SortDirection == SortDirection.Descending
                    ? query.OrderByDescending(u => _userRepository.CountPostsByUsername(u.Username))
                    : query.OrderBy(u => _userRepository.CountPostsByUsername(u.Username)),
                "threads" => request.SortDirection == SortDirection.Descending
                    ? query.OrderByDescending(u => _userRepository.CountThreadsByUsername(u.Username))
                    : query.OrderBy(u => _userRepository.CountThreadsByUsername(u.Username)),
                _ => query.OrderBy(u => u.Username)
            };

            var totalMembers = query.Count();
            var pageSize = Math.Max(1, request.PageSize);
            var page = Math.Max(1, request.Page);
            var totalPages = (int)Math.Ceiling(totalMembers / (double)pageSize);
            var skip = (page - 1) * pageSize;

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
            var user = _userRepository.GetById(userId);

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

            if (!User.IsInRole("ApiClient"))
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId != currentUserId)
                {
                    return Forbid();
                }
            }

            var user = _userRepository.GetById(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!user.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) &&
                _userRepository.ExistsWithUsername(request.Username, userId))
            {
                return BadRequest(new { message = "Username is already taken" });
            }

            if (!user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase) &&
                _userRepository.ExistsWithEmail(request.Email, userId))
            {
                return BadRequest(new { message = "Email is already registered" });
            }

            user.Username = request.Username;
            user.Email = request.Email;
            user.Signature = request.Signature;
            user.Bio = request.Bio;
            user.AvatarUrl = request.AvatarUrl;
            user.ShowEmail = request.ShowEmail;
            _userRepository.Update(user);

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

            if (!User.IsInRole("ApiClient"))
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId != currentUserId)
                {
                    return Forbid();
                }
            }

            var user = _userRepository.GetById(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            _userRepository.Update(user);
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

            var user = _userRepository.GetById(request.UserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                return BadRequest(new { message = "Cannot change your own role" });
            }

            user.Role = request.NewRole;
            _userRepository.Update(user);
            return Ok(new { message = "User role updated successfully" });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("{id}/anonymize")]
        public ActionResult Anonymize(int id)
        {
            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                return BadRequest(new { message = "Cannot anonymize your own account" });
            }

            if (user.Role == UserRole.Deleted)
            {
                return BadRequest(new { message = "User is already deleted" });
            }

            // Overwrite the identifying fields with deterministic placeholders
            // (deterministic so Username/Email still satisfy their unique
            // constraints) rather than deleting the row, so existing posts/
            // threads/messages keep a valid author and show the generic name
            // via the same joins/mappings used for every other user - no
            // separate redaction logic needed anywhere else.
            user.Username = $"DeletedUser{user.Id}";
            user.Email = $"deleted+{user.Id}@deleted.local";
            user.PasswordHash = HashPassword(Guid.NewGuid().ToString("N"));
            user.Signature = null;
            user.Bio = null;
            user.AvatarUrl = null;
            user.ShowEmail = false;
            user.Role = UserRole.Deleted;
            _userRepository.Update(user);

            return Ok(new { message = "User anonymized successfully" });
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
                ThreadCount = _userRepository.CountThreadsByUsername(u.Username),
                PostCount = _userRepository.CountPostsByUsername(u.Username),
                Role = u.Role,
                Signature = u.Signature,
                Bio = u.Bio,
                AvatarUrl = u.AvatarUrl,
                ShowEmail = u.ShowEmail
            };
        }
    }
}
