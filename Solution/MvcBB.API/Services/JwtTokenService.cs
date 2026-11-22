using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Creates and validates every JWT the API hands out, so issuing and
    /// checking always agree.
    ///
    /// - Every token names this API as its issuer and has an audience, and both
    ///   are checked on the way back in.
    /// - Jwt:SecretKey is read as UTF-8 (the old ASCII reading turned every
    ///   non-ASCII character into '?', silently weakening the key).
    /// - User tokens and the App's service token are signed with two different
    ///   keys derived from Jwt:SecretKey, and have different audiences. A token
    ///   is only accepted if its key and audience match its kind, so one kind
    ///   can never be passed off as the other.
    ///
    /// Jwt:Issuer, Jwt:Audience and Jwt:ServiceAudience are optional and have
    /// sensible defaults, so existing appsettings files keep working. Tokens
    /// issued before this change are no longer accepted, so everyone has to
    /// sign in again once after it's deployed.
    /// </summary>
    public class JwtTokenService
    {
        public const string UserKeyId = "mvcbb-user";
        public const string ServiceKeyId = "mvcbb-service";
        public const string ServiceRole = "ApiClient";

        /// <summary>
        /// Claim on every user token: a fingerprint of the account's password
        /// hash, role and lock state when the token was issued. See
        /// ComputeSessionStamp / CheckUserSession.
        /// </summary>
        public const string SessionStampClaim = "sstamp";

        private readonly IConfiguration _configuration;
        private readonly byte[] _sessionStampKey;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;

            var secret = configuration["Jwt:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            {
                throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
            }

            var secretBytes = Encoding.UTF8.GetBytes(secret);
            UserSigningKey = new SymmetricSecurityKey(DeriveKey(secretBytes, "MvcBB user tokens v1")) { KeyId = UserKeyId };
            _sessionStampKey = DeriveKey(secretBytes, "MvcBB session stamps v1");
            ServiceSigningKey = new SymmetricSecurityKey(DeriveKey(secretBytes, "MvcBB service tokens v1")) { KeyId = ServiceKeyId };

            Issuer = configuration["Jwt:Issuer"] is { Length: > 0 } issuer ? issuer : "MvcBB.API";
            UserAudience = configuration["Jwt:Audience"] is { Length: > 0 } audience ? audience : "MvcBB.App";
            ServiceAudience = configuration["Jwt:ServiceAudience"] is { Length: > 0 } serviceAudience ? serviceAudience : "MvcBB.App.Service";
        }

        public string Issuer { get; }
        public string UserAudience { get; }
        public string ServiceAudience { get; }
        public SymmetricSecurityKey UserSigningKey { get; }
        public SymmetricSecurityKey ServiceSigningKey { get; }

        // HMAC-SHA256(secret, purpose): two independent keys from one secret.
        private static byte[] DeriveKey(byte[] secret, string purpose) =>
            HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(purpose));

        public TimeSpan UserTokenLifetime
        {
            get
            {
                var hours = _configuration.GetValue<int>("Jwt:TokenExpirationHours");
                return TimeSpan.FromHours(hours > 0 ? hours : 1);
            }
        }

        public string CreateUserToken(User user, IEnumerable<Claim>? extraClaims = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new(SessionStampClaim, ComputeSessionStamp(user))
            };
            if (extraClaims != null)
            {
                claims.AddRange(extraClaims);
            }

            return Write(claims, UserAudience, UserSigningKey, DateTime.UtcNow.Add(UserTokenLifetime));
        }

        public (string Token, DateTime ExpiresAt) CreateServiceToken(string clientId)
        {
            var expiresAt = DateTime.UtcNow.AddHours(1);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, clientId),
                new Claim(ClaimTypes.Role, ServiceRole)
            };
            return (Write(claims, ServiceAudience, ServiceSigningKey, expiresAt), expiresAt);
        }

        private string Write(IEnumerable<Claim> claims, string audience, SecurityKey key, DateTime expiresAt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = Issuer,
                Audience = audience,
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            });
            return handler.WriteToken(token);
        }

        /// <summary>
        /// A keyed fingerprint of everything that should end a user's existing
        /// sessions when it changes: their password (changed or reset), their
        /// role (promoted, demoted or anonymized) and their lock. It's computed
        /// from the user row itself, so no extra database column is needed, and
        /// it's keyed with a secret so it reveals nothing about the password
        /// hash.
        /// </summary>
        public string ComputeSessionStamp(User user)
        {
            var material = $"{user.Id}|{user.PasswordHash}|{(int)user.Role}|{user.LockedUntil?.Ticks ?? 0}";
            var mac = HMACSHA256.HashData(_sessionStampKey, Encoding.UTF8.GetBytes(material));
            return Base64UrlEncoder.Encode(mac.AsSpan(0, 16).ToArray());
        }

        /// <summary>
        /// Runs on every request that carries a user token: looks the user up
        /// and refuses the token if the account is gone, is locked, or its
        /// password, role or lock has changed since the token was issued.
        /// Returns an error message, or null if the session is still valid.
        /// </summary>
        public string? CheckUserSession(TokenValidatedContext context)
        {
            var principal = context.Principal;
            if (principal == null || principal.IsInRole(ServiceRole))
            {
                return null;
            }

            if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                return "Token has no user.";
            }

            var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var user = users.GetById(userId);
            if (user == null)
            {
                return "User no longer exists.";
            }

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                return "User is locked.";
            }

            var stamp = principal.FindFirst(SessionStampClaim)?.Value;
            if (stamp == null || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(stamp), Encoding.UTF8.GetBytes(ComputeSessionStamp(user))))
            {
                return "Session has been ended.";
            }

            return null;
        }

        public TokenValidationParameters CreateValidationParameters() => new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = new[] { UserSigningKey, ServiceSigningKey },
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudiences = new[] { UserAudience, ServiceAudience },
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };

        /// <summary>
        /// Called once a token's signature, issuer, audience and expiry have
        /// passed: makes sure a service token was signed with the service key
        /// for the service audience, and a user token with the user key for the
        /// user audience. Returns an error message, or null if it's fine.
        /// </summary>
        public string? CheckTokenKind(TokenValidatedContext context)
        {
            var isService = context.Principal?.IsInRole(ServiceRole) == true;
            var keyId = context.SecurityToken?.SigningKey?.KeyId;
            var audiences = context.SecurityToken switch
            {
                JsonWebToken jwt => jwt.Audiences,
                JwtSecurityToken jwt => jwt.Audiences,
                _ => Enumerable.Empty<string>()
            };

            var expectedKeyId = isService ? ServiceKeyId : UserKeyId;
            var expectedAudience = isService ? ServiceAudience : UserAudience;

            if (keyId != expectedKeyId || !audiences.Contains(expectedAudience))
            {
                return "Token kind does not match its signing key or audience.";
            }

            return null;
        }
    }
}
