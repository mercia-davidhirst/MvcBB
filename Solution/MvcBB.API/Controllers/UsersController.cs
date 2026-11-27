using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MvcBB.Shared.Models.AdminEmail;
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
        private readonly JwtTokenService _jwtTokenService;
        private readonly IAdminEmailRepository _adminEmailRepository;
        private readonly IModerationLogger _moderationLog;
        private readonly RefreshTokenService _refreshTokens;

        public UsersController(
            IConfiguration configuration,
            IUserRepository userRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IEmailSender emailSender,
            IRankRepository rankRepository,
            IDisplaySettingsService displaySettingsService,
            IPasswordPolicyService passwordPolicyService,
            JwtTokenService jwtTokenService,
            IAdminEmailRepository adminEmailRepository,
            IModerationLogger moderationLog,
            RefreshTokenService refreshTokens)
        {
            _moderationLog = moderationLog;
            _refreshTokens = refreshTokens;
            _configuration = configuration;
            _userRepository = userRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailSender = emailSender;
            _rankRepository = rankRepository;
            _displaySettingsService = displaySettingsService;
            _passwordPolicyService = passwordPolicyService;
            _jwtTokenService = jwtTokenService;
            _adminEmailRepository = adminEmailRepository;
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

        // Signed with the user key, for the user audience - see JwtTokenService.
        private string GenerateJwtToken(User user) => _jwtTokenService.CreateUserToken(user);

        [AllowAnonymous]
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
            var expiresAt = DateTime.UtcNow.Add(_jwtTokenService.UserTokenLifetime);

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

        [AllowAnonymous]
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

            // "Remember me": a refresh token the App swaps for fresh access
            // tokens, so the member stays signed in - see RefreshTokenService.
            var refresh = request.RememberMe ? _refreshTokens.Issue(user, request.IpAddress) : null;
            return BuildAuthResponse(user, refresh);
        }

        /// <summary>
        /// Swaps a "Remember me" refresh token for a new access token and a new
        /// refresh token (the old one is spent). 401 if the token is unknown,
        /// expired, revoked or reused, or the account has been locked, demoted,
        /// anonymized or had its password changed since; 409 if another request
        /// rotated the same token a moment ago.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("refresh")]
        public ActionResult<UserAuthResponse> Refresh(RefreshTokenRequest request)
        {
            var result = _refreshTokens.Rotate(request.RefreshToken, request.IpAddress);
            return result.Outcome switch
            {
                RefreshOutcome.Success => BuildAuthResponse(result.User!, result.Token),
                RefreshOutcome.Conflict => Conflict(new { message = "This session was renewed by another request." }),
                _ => Unauthorized(new { message = "Your session has ended. Please log in again." })
            };
        }

        /// <summary>
        /// Signing out of a "Remember me" session: revokes that device's refresh
        /// tokens. Knowing the token is the proof, so no other credentials are
        /// needed, and the reply is the same whether or not it matched.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("logout")]
        public ActionResult Logout(RefreshTokenRequest request)
        {
            _refreshTokens.Revoke(request.RefreshToken);
            return NoContent();
        }

        private UserAuthResponse BuildAuthResponse(User user, IssuedRefreshToken? refresh) => new()
        {
            Token = GenerateJwtToken(user),
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
            ExpiresAt = DateTime.UtcNow.Add(_jwtTokenService.UserTokenLifetime),
            RefreshToken = refresh?.Token,
            RefreshTokenExpiresAt = refresh?.ExpiresAt
        };

        [AllowAnonymous]
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

                await _emailSender.SendAsync(new EmailMessage
                {
                    To = user.Email,
                    Subject = "Reset your MvcBB password",
                    Body = $"Click the link below to reset your password. This link expires in {expirationMinutes} minutes.\n\n{resetLink}\n\nIf you didn't request this, you can ignore this email.",
                    Kind = EmailKind.PasswordReset
                });
            }

            return Ok(new { message = "If that email is registered, a password reset link has been sent." });
        }

        [AllowAnonymous]
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
            _refreshTokens.RevokeAllForUser(user.Id);

            return Ok(new { message = "Password reset successfully." });
        }

        // Unauthenticated and was previously completely unbounded (every row
        // in the Users table, in one response) - bounded here the same way
        // GetMembers is, so it can't become a full-table dump as the member
        // base grows. Capped lower than the admin variant below since this
        // one needs no authorization at all.
        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<UserResponse>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var (users, _) = _userRepository.GetMembersPage(role: null, search: null, sortBy: "username", sortDescending: false, skip: (page - 1) * pageSize, take: pageSize);
            return Ok(MapToUserResponses(users));
        }

        // For the admin User Management page specifically - this is the only
        // listing endpoint that includes email, and it's locked to real
        // Administrators so the unauthenticated GetUsers() above can't be used
        // to harvest every member's email address. Defaults to a generous
        // page size (large enough to still return "everyone" for a typical
        // community) rather than GetUsers()'s smaller default, since admins
        // managing users need the full picture; Account/Users.cshtml doesn't
        // have paging controls yet, so this keeps it working unchanged while
        // no longer being capable of an unbounded full-table scan.
        [Authorize(Roles = "Administrator")]
        [HttpGet("admin")]
        public ActionResult<IEnumerable<UserResponse>> GetUsersForAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 500)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 1000);
            var (users, _) = _userRepository.GetMembersPage(role: null, search: null, sortBy: "username", sortDescending: false, skip: (page - 1) * pageSize, take: pageSize);
            return Ok(MapToUserResponses(users, includeEmail: true));
        }

        // Paged, searchable, sortable listing for the admin User Management
        // page - the same filtering as GetMembers below, but including the
        // admin-only fields (email, lock status, last IP), so it's locked to
        // real Administrators like GetUsersForAdmin above.
        [Authorize(Roles = "Administrator")]
        [HttpGet("admin/members")]
        public ActionResult<MemberResponse> GetMembersForAdmin([FromQuery] MemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var pageSize = Math.Max(1, request.PageSize);
            var page = Math.Max(1, request.Page);

            var (users, totalMembers) = _userRepository.GetMembersPage(
                request.Role,
                string.IsNullOrWhiteSpace(request.Search) ? null : request.Search,
                request.SortBy,
                request.SortDirection == SortDirection.Descending,
                (page - 1) * pageSize,
                pageSize,
                searchEmail: true);

            return Ok(new MemberResponse
            {
                Members = MapToUserResponses(users, includeEmail: true),
                TotalMembers = totalMembers,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalMembers / (double)pageSize)
            });
        }

        // A single user with the admin-only fields, for the admin per-user
        // management page. GetById below stays public and leaves them out.
        [Authorize(Roles = "Administrator")]
        [HttpGet("admin/{id:int}")]
        public ActionResult<UserResponse> GetByIdForAdmin(int id)
        {
            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return WithAdminEmailPreference(MapToUserResponse(user, includeEmail: true));
        }

        // Backs the homepage statistics panel - a couple of aggregate COUNTs
        // instead of the caller loading every user to total them up itself.
        [AllowAnonymous]
        [HttpGet("stats")]
        public ActionResult<UserStatsResponse> GetStats([FromQuery] int activeSinceDays = 7)
        {
            var activeSince = DateTime.UtcNow.AddDays(-Math.Max(0, activeSinceDays));
            var (totalMembers, activeMembers) = _userRepository.GetStats(activeSince);
            return Ok(new UserStatsResponse { TotalMembers = totalMembers, ActiveMembers = activeMembers });
        }

        // Public member-profile lookup - e.g. clicking a username elsewhere in the
        // forum. Email is excluded since this isn't the account owner viewing it.
        [AllowAnonymous]
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

        [AllowAnonymous]
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

        [AllowAnonymous]
        [HttpGet("members")]
        public ActionResult<MemberResponse> GetMembers([FromQuery] MemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var pageSize = Math.Max(1, request.PageSize);
            var page = Math.Max(1, request.Page);
            var skip = (page - 1) * pageSize;

            var (users, totalMembers) = _userRepository.GetMembersPage(
                request.Role,
                string.IsNullOrWhiteSpace(request.Search) ? null : request.Search,
                request.SortBy,
                request.SortDirection == SortDirection.Descending,
                skip,
                pageSize);

            var totalPages = (int)Math.Ceiling(totalMembers / (double)pageSize);

            return Ok(new MemberResponse
            {
                Members = MapToUserResponses(users),
                TotalMembers = totalMembers,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Up to 8 members whose username contains the query, those starting
        /// with it first - for the @mention autocomplete in the post editor.
        /// Signed-in members only; anonymized accounts and the System account
        /// are left out.
        /// </summary>
        [Authorize]
        [HttpGet("mention-suggestions")]
        public ActionResult<IEnumerable<string>> GetMentionSuggestions([FromQuery] string? q)
        {
            var query = (q ?? string.Empty).Trim().TrimStart('@').Trim('"');
            if (query.Length < 1 || query.Length > 50)
            {
                return Ok(Array.Empty<string>());
            }

            var (users, _) = _userRepository.GetMembersPage(null, query, "username", false, 0, 30);
            var names = users
                .Where(u => u.Role != UserRole.Deleted && u.Username != SystemUserProvider.Username)
                .Select(u => u.Username)
                .OrderBy(name => name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();
            return Ok(names);
        }

        // Lets MvcBB.App confirm, on each request, that a signed-in member's
        // session is still valid. The real check happens while the token is
        // authenticated (JwtTokenService.CheckUserSession); getting here at
        // all means it passed.
        [Authorize]
        [HttpGet("session")]
        public ActionResult CheckSession()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier) == null ? Unauthorized() : NoContent();
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

            return WithAdminEmailPreference(MapToUserResponse(user, includeEmail: true));
        }

        [Authorize]
        [HttpPut("{userId}")]
        public ActionResult UpdateProfile(int userId, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Only ever the caller's own account. There is deliberately no
            // exception for the ApiClient service token (it has no user id, so
            // it can't pass this): admin changes to other accounts go through
            // the Administrator-only endpoints below.
            var currentUserId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUserId) ? parsedUserId : 0;
            if (currentUserId == 0 || userId != currentUserId)
            {
                return Forbid();
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

            var emailChanged = !user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase);
            if (emailChanged &&
                (string.IsNullOrEmpty(request.CurrentPassword) || !VerifyPassword(request.CurrentPassword, user.PasswordHash)))
            {
                return BadRequest(new { message = "Enter your current password to change your email address." });
            }

            if (emailChanged && _userRepository.ExistsWithEmail(request.Email, userId))
            {
                return BadRequest(new { message = "Email is already registered" });
            }

            user.Username = request.Username;
            user.Email = request.Email;
            user.Signature = request.Signature;
            user.Bio = request.Bio;
            user.AvatarUrl = request.AvatarUrl;
            user.ShowEmail = request.ShowEmail;
            if (request.UpdateBirthday)
            {
                var birthdayProblem = Birthday.Validate(request.BirthDay, request.BirthMonth, request.BirthYear, DateTime.UtcNow);
                if (birthdayProblem != null)
                {
                    return BadRequest(new { message = birthdayProblem });
                }

                var hasBirthday = request.BirthDay.HasValue && request.BirthMonth.HasValue;
                user.BirthDay = hasBirthday ? request.BirthDay : null;
                user.BirthMonth = hasBirthday ? request.BirthMonth : null;
                user.BirthYear = hasBirthday ? request.BirthYear : null;
            }
            if (request.AutoJumpToUnread.HasValue)
            {
                user.AutoJumpToUnread = request.AutoJumpToUnread.Value;
            }
            _userRepository.Update(user);

            // Null (an older client that doesn't send it) leaves it as it is.
            if (request.AllowAdminEmails.HasValue)
            {
                _adminEmailRepository.SetOptedOut(userId, !request.AllowAdminEmails.Value);
            }

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

            // Only ever the caller's own account. There is deliberately no
            // exception for the ApiClient service token (it has no user id, so
            // it can't pass this): admin changes to other accounts go through
            // the Administrator-only endpoints below.
            var currentUserId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUserId) ? parsedUserId : 0;
            if (currentUserId == 0 || userId != currentUserId)
            {
                return Forbid();
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

            // The new password ends every existing session, including the one
            // making this request, and every "Remember me" on other devices,
            // so hand back a fresh token (and, if this session was remembered,
            // a fresh refresh token) for it to carry on.
            _refreshTokens.RevokeAllForUser(user.Id);
            var refresh = request.KeepSignedIn ? _refreshTokens.Issue(user, null) : null;
            return Ok(new
            {
                token = GenerateJwtToken(user),
                expiresAt = DateTime.UtcNow.Add(_jwtTokenService.UserTokenLifetime),
                refreshToken = refresh?.Token,
                refreshTokenExpiresAt = refresh?.ExpiresAt
            });
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

            var isLocked = user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow;
            if (isLocked)
            {
                // A lock ends "Remember me" for good, not just until it lifts.
                _refreshTokens.RevokeAllForUser(user.Id);
            }
            _moderationLog.Log(isLocked ? MvcBB.Shared.Models.Moderation.ModerationActions.LockUser : MvcBB.Shared.Models.Moderation.ModerationActions.UnlockUser,
                "User", user.Id, user.Id,
                isLocked ? $"Locked {user.Username} until {user.LockedUntil!.Value:yyyy-MM-dd HH:mm} UTC" : $"Unlocked {user.Username}");

            var message = isLocked
                ? $"User locked until {user.LockedUntil!.Value:u}."
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

            var oldRole = user.Role;
            user.Role = request.NewRole;
            _userRepository.Update(user);

            if (oldRole != user.Role)
            {
                _refreshTokens.RevokeAllForUser(user.Id);
                _moderationLog.Log(MvcBB.Shared.Models.Moderation.ModerationActions.ChangeRole, "User", user.Id, user.Id,
                    $"Changed {user.Username} from {oldRole} to {user.Role}");
            }

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

            var oldTitle = user.CustomTitle;
            user.CustomTitle = string.IsNullOrWhiteSpace(request.CustomTitle) ? null : request.CustomTitle.Trim();
            _userRepository.Update(user);

            if (oldTitle != user.CustomTitle)
            {
                _moderationLog.Log(MvcBB.Shared.Models.Moderation.ModerationActions.SetCustomTitle, "User", user.Id, user.Id,
                    user.CustomTitle == null ? $"Cleared {user.Username}'s custom title" : $"Set {user.Username}'s custom title to \"{user.CustomTitle}\"");
            }

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

            _refreshTokens.RevokeAllForUser(user.Id);

            // Logged before the name is overwritten, so the log says who it was.
            _moderationLog.Log(MvcBB.Shared.Models.Moderation.ModerationActions.AnonymizeUser, "User", user.Id, user.Id,
                $"Anonymized {user.Username} (now DeletedUser{user.Id})");

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
            user.BirthDay = null;
            user.BirthMonth = null;
            user.BirthYear = null;
            user.Role = UserRole.Deleted;
            _userRepository.Update(user);

            return Ok(new { message = "User anonymized successfully" });
        }

        // Only the member themselves (profile) and admins (per-user page) see
        // whether they get admin emails, so it's filled in only there.
        private UserResponse WithAdminEmailPreference(UserResponse response)
        {
            response.AllowAdminEmails = !_adminEmailRepository.IsOptedOut(response.Id);
            return response;
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
                    AutoJumpToUnread = u.AutoJumpToUnread,
                    BirthDay = u.BirthDay,
                    BirthMonth = u.BirthMonth,
                    BirthYear = u.BirthYear,
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
