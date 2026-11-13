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
using MvcBB.Shared.Services;
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
        private readonly IRankRepository _rankRepository;
        private readonly IDisplaySettingsService _displaySettingsService;
        private readonly IPasswordPolicyService _passwordPolicyService;

        public UsersController(
            IConfiguration configuration,
            IUserRepository userRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IEmailSender emailSender,
            IRankRepository rankRepository,
            IDisplaySettingsService displaySettingsService,
            IPasswordPolicyService passwordPolicyService)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailSender = emailSender;
            _rankRepository = rankRepository;
            _displaySettingsService = displaySettingsService;
            _passwordPolicyService = passwordPolicyService;
        }

        private bool TryValidatePasswordPolicy(string password, string fieldKey, out ActionResult? errorResult)
        {
            if (PasswordPolicyValidator.TryValidate(password, _passwordPolicyService.GetPasswordPolicy(), out var error))
            {
                errorResult = null;
                return true;
            }

            ModelState.AddModelError(fieldKey, error!);
            errorResult = BadRequest(ModelState);
            return false;
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

            if (!_displaySettingsService.GetDisplaySettings().RegistrationEnabled)
            {
                return BadRequest(new { message = "Registration is currently disabled." });
            }

            if (!TryValidatePasswordPolicy(request.Password, nameof(request.Password), out var policyError))
            {
                return policyError!;
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

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = $"This account is locked until {user.LockedUntil.Value:u}." });
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;
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
                    Role = user.Role,
                    MustChangePassword = user.MustChangePassword
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

            if (!TryValidatePasswordPolicy(request.NewPassword, nameof(request.NewPassword), out var policyError))
            {
                return policyError!;
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            _userRepository.Update(user);
            _passwordResetTokenRepository.MarkUsed(token.Id);

            return Ok(new { message = "Password reset successfully." });
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserResponse>> GetUsers()
        {
            return Ok(MapToUserResponses(_userRepository.GetAll()));
        }

        // For the admin User Management page specifically - this is the only
        // listing endpoint that includes email, and it's locked to real
        // Administrators so the unauthenticated GetUsers() above can't be used
        // to harvest every member's email address.
        [Authorize(Roles = "Administrator")]
        [HttpGet("admin")]
        public ActionResult<IEnumerable<UserResponse>> GetUsersForAdmin()
        {
            return Ok(MapToUserResponses(_userRepository.GetAll(), includeEmail: true));
        }

        // Public member-profile lookup - e.g. clicking a username elsewhere in the
        // forum. Email is excluded since this isn't the account owner viewing it.
        [HttpGet("by-username/{username}")]
        public ActionResult<UserResponse> GetByUsername(string username)
        {
            var user = _userRepository.GetByUsername(username);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return MapToUserResponse(user, includeEmail: false);
        }

        [HttpGet("{id:int}")]
        public ActionResult<UserResponse> GetById(int id)
        {
            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return MapToUserResponse(user, includeEmail: false);
        }

        [HttpGet("members")]
        public ActionResult<MemberResponse> GetMembers([FromQuery] MemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var filtered = _userRepository.GetAll().AsEnumerable();

            if (request.Role.HasValue)
            {
                filtered = filtered.Where(u => u.Role == request.Role.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLower();
                filtered = filtered.Where(u => u.Username.ToLower().Contains(search));
            }

            var filteredUsers = filtered.ToList();

            // "posts"/"threads" sorting needs each user's count, which the
            // repository can only give per-username - fetch all of them in
            // one batched call each, rather than one count call per user.
            IEnumerable<User> ordered;
            var sortBy = request.SortBy?.ToLower();
            if (sortBy is "posts" or "threads")
            {
                var usernames = filteredUsers.Select(u => u.Username).ToList();
                var counts = sortBy == "posts"
                    ? _userRepository.GetPostCountsByUsernames(usernames)
                    : _userRepository.GetThreadCountsByUsernames(usernames);

                ordered = request.SortDirection == SortDirection.Descending
                    ? filteredUsers.OrderByDescending(u => counts.TryGetValue(u.Username, out var c) ? c : 0)
                    : filteredUsers.OrderBy(u => counts.TryGetValue(u.Username, out var c) ? c : 0);
            }
            else
            {
                ordered = sortBy switch
                {
                    "username" => request.SortDirection == SortDirection.Descending
                        ? filteredUsers.OrderByDescending(u => u.Username)
                        : filteredUsers.OrderBy(u => u.Username),
                    "joined" => request.SortDirection == SortDirection.Descending
                        ? filteredUsers.OrderByDescending(u => u.CreatedAt)
                        : filteredUsers.OrderBy(u => u.CreatedAt),
                    _ => filteredUsers.OrderBy(u => u.Username)
                };
            }

            var totalMembers = filteredUsers.Count;
            var pageSize = Math.Max(1, request.PageSize);
            var page = Math.Max(1, request.Page);
            var totalPages = (int)Math.Ceiling(totalMembers / (double)pageSize);
            var skip = (page - 1) * pageSize;

            var memberUsers = ordered.Skip(skip).Take(pageSize).ToList();
            var members = MapToUserResponses(memberUsers);

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

            if (!TryValidatePasswordPolicy(request.NewPassword, nameof(request.NewPassword), out var policyError))
            {
                return policyError!;
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            _userRepository.Update(user);
            return NoContent();
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("{id}/require-password-change")]
        public ActionResult RequirePasswordChange(int id)
        {
            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.MustChangePassword = true;
            _userRepository.Update(user);
            return Ok(new { message = "The user will be required to set a new password on their next login." });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}/email")]
        public ActionResult UpdateEmail(int id, AdminSetEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase) &&
                _userRepository.ExistsWithEmail(request.Email, id))
            {
                return BadRequest(new { message = "Email is already registered" });
            }

            user.Email = request.Email;
            _userRepository.Update(user);
            return Ok(new { message = "Email updated successfully" });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}/lock")]
        public ActionResult LockUser(int id, LockUserRequest request)
        {
            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId && request.LockedUntil.HasValue && request.LockedUntil.Value > DateTime.UtcNow)
            {
                return BadRequest(new { message = "Cannot lock your own account" });
            }

            user.LockedUntil = request.LockedUntil;
            _userRepository.Update(user);

            var message = user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow
                ? $"User locked until {user.LockedUntil.Value:u}."
                : "User unlocked.";
            return Ok(new { message });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("admin-create")]
        public ActionResult<UserResponse> AdminCreateUser(AdminCreateUserRequest request)
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

            if (!TryValidatePasswordPolicy(request.Password, nameof(request.Password), out var policyError))
            {
                return policyError!;
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                Role = request.Role,
                MustChangePassword = request.MustChangePassword
            };

            user = _userRepository.Add(user);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, MapToUserResponse(user, includeEmail: true));
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("require-password-change-all")]
        public ActionResult RequirePasswordChangeForAll()
        {
            var affected = 0;
            foreach (var user in _userRepository.GetAll())
            {
                if (user.Role == UserRole.Deleted || user.MustChangePassword)
                {
                    continue;
                }

                user.MustChangePassword = true;
                _userRepository.Update(user);
                affected++;
            }

            return Ok(new { message = $"{affected} user(s) will be required to set a new password on their next login.", affectedCount = affected });
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
        [HttpPut("{id}/custom-title")]
        public ActionResult SetCustomTitle(int id, SetCustomTitleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.CustomTitle = string.IsNullOrWhiteSpace(request.CustomTitle) ? null : request.CustomTitle.Trim();
            _userRepository.Update(user);
            return Ok(new { message = "Custom title updated successfully" });
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

        private UserResponse MapToUserResponse(User u, bool includeEmail = false) =>
            MapToUserResponses(new[] { u }, includeEmail).Single();

        /// <summary>
        /// Maps a page's worth of users to responses with a fixed, small
        /// number of batched queries total (post/thread counts, and the rank
        /// ladder fetched once) instead of two count queries plus a full
        /// ranks-table fetch per user.
        /// </summary>
        private List<UserResponse> MapToUserResponses(IReadOnlyList<User> users, bool includeEmail = false)
        {
            if (users.Count == 0)
            {
                return new List<UserResponse>();
            }

            var usernames = users.Select(u => u.Username).ToList();
            var postCounts = _userRepository.GetPostCountsByUsernames(usernames);
            var threadCounts = _userRepository.GetThreadCountsByUsernames(usernames);
            var ranks = _rankRepository.GetAll();

            return users.Select(u =>
            {
                var postCount = postCounts.TryGetValue(u.Username, out var pc) ? pc : 0;
                var hasCustomTitle = !string.IsNullOrEmpty(u.CustomTitle);
                var rank = hasCustomTitle ? null : RankCalculator.GetRank(ranks, postCount);

                return new UserResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = includeEmail ? u.Email : string.Empty,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    ThreadCount = threadCounts.TryGetValue(u.Username, out var tc) ? tc : 0,
                    PostCount = postCount,
                    Role = u.Role,
                    Signature = u.Signature,
                    Bio = u.Bio,
                    AvatarUrl = u.AvatarUrl,
                    ShowEmail = u.ShowEmail,
                    RankTitle = hasCustomTitle ? u.CustomTitle : rank?.Title,
                    RankImagePath = rank?.ImagePath,
                    CustomTitle = u.CustomTitle,
                    MustChangePassword = u.MustChangePassword,
                    // Lock status/IP are only ever shown alongside email, i.e. only on
                    // the admin-only GetUsersForAdmin listing - not the public ones.
                    LockedUntil = includeEmail ? u.LockedUntil : null,
                    LastLoginIp = includeEmail ? u.LastLoginIp : null
                };
            }).ToList();
        }
    }
}
