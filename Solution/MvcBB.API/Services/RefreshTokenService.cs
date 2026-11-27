using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.Services
{
    public enum RefreshOutcome
    {
        /// <summary>Rotated: the old token is spent and a new one was issued.</summary>
        Success,

        /// <summary>Unknown, revoked or blank token.</summary>
        Invalid,

        /// <summary>Past its expiry.</summary>
        Expired,

        /// <summary>
        /// Already used, outside the grace window: the token was copied, so
        /// its whole family has been revoked.
        /// </summary>
        Reused,

        /// <summary>
        /// The account is gone, anonymized or locked, or its password or role
        /// changed since the token was issued. The family has been revoked.
        /// </summary>
        SessionEnded,

        /// <summary>
        /// Another request rotated this token a moment ago (two requests from
        /// the same browser at once). Nothing was revoked; the caller should
        /// carry on with whatever the other request got.
        /// </summary>
        Conflict
    }

    public sealed record IssuedRefreshToken(string Token, DateTime ExpiresAt);

    public sealed record RefreshResult(RefreshOutcome Outcome, User? User = null, IssuedRefreshToken? Token = null);

    /// <summary>
    /// "Remember me" refresh tokens.
    ///
    /// - 256 random bits, handed to the App once and stored here only as a
    ///   SHA-256 hash, so a database leak gives nobody a usable token.
    /// - Single use and rotated: each refresh spends the token and issues a
    ///   new one in the same family (one family = one sign-in on one device).
    ///   The new token's expiry is a fresh Jwt:RefreshTokenExpirationDays, so
    ///   a member who visits at least that often stays signed in.
    /// - Reuse detection: a spent token presented again means someone copied
    ///   it, so the whole family is revoked and both copies stop working. A
    ///   short grace window (a few seconds) lets two simultaneous requests
    ///   from the same browser through without tripping this.
    /// - Each token records the account's session stamp, so a password
    ///   change, role change, lock or anonymization ends it, the same way it
    ///   ends access tokens. Those actions also revoke the member's tokens
    ///   outright, and signing out revokes that device's family.
    /// </summary>
    public class RefreshTokenService
    {
        private static readonly TimeSpan ReuseGrace = TimeSpan.FromSeconds(30);

        private readonly IRefreshTokenRepository _tokens;
        private readonly IUserRepository _users;
        private readonly JwtTokenService _jwt;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _time;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            IRefreshTokenRepository tokens,
            IUserRepository users,
            JwtTokenService jwt,
            IConfiguration configuration,
            TimeProvider time,
            ILogger<RefreshTokenService> logger)
        {
            _tokens = tokens;
            _users = users;
            _jwt = jwt;
            _configuration = configuration;
            _time = time;
            _logger = logger;
        }

        /// <summary>Jwt:RefreshTokenExpirationDays, default 30, between 1 and 365.</summary>
        public TimeSpan Lifetime =>
            TimeSpan.FromDays(Math.Clamp(_configuration.GetValue("Jwt:RefreshTokenExpirationDays", 30), 1, 365));

        private DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

        public static string Hash(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        /// <summary>Issues a new token, starting a new family unless one is given.</summary>
        public IssuedRefreshToken Issue(User user, string? ipAddress, Guid? familyId = null)
        {
            var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
            var now = UtcNow;
            var expiresAt = now.Add(Lifetime);

            _tokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = Hash(raw),
                FamilyId = familyId ?? Guid.NewGuid(),
                SessionStamp = _jwt.ComputeSessionStamp(user),
                CreatedAt = now,
                ExpiresAt = expiresAt,
                CreatedIp = Truncate(ipAddress, 45)
            });

            return new IssuedRefreshToken(raw, expiresAt);
        }

        public RefreshResult Rotate(string? rawToken, string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
            {
                return new RefreshResult(RefreshOutcome.Invalid);
            }

            var token = _tokens.GetByTokenHash(Hash(rawToken));
            if (token == null || token.RevokedAt != null)
            {
                return new RefreshResult(RefreshOutcome.Invalid);
            }

            var now = UtcNow;
            if (token.UsedAt != null)
            {
                if (now - token.UsedAt.Value < ReuseGrace)
                {
                    return new RefreshResult(RefreshOutcome.Conflict);
                }

                _tokens.RevokeFamily(token.FamilyId);
                _logger.LogWarning("A spent refresh token for user {UserId} was presented again; that sign-in has been revoked.", token.UserId);
                return new RefreshResult(RefreshOutcome.Reused);
            }

            if (token.ExpiresAt <= now)
            {
                return new RefreshResult(RefreshOutcome.Expired);
            }

            var user = _users.GetById(token.UserId);
            if (user == null
                || user.Role == UserRole.Deleted
                || (user.LockedUntil.HasValue && user.LockedUntil.Value > now)
                || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(token.SessionStamp), Encoding.UTF8.GetBytes(_jwt.ComputeSessionStamp(user))))
            {
                _tokens.RevokeFamily(token.FamilyId);
                return new RefreshResult(RefreshOutcome.SessionEnded);
            }

            if (!_tokens.MarkUsed(token.Id, now))
            {
                // Lost a race with another request presenting the same token.
                return new RefreshResult(RefreshOutcome.Conflict);
            }

            return new RefreshResult(RefreshOutcome.Success, user, Issue(user, ipAddress, token.FamilyId));
        }

        /// <summary>Signs out one device: revokes the family the token belongs to.</summary>
        public void Revoke(string? rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
            {
                return;
            }

            var token = _tokens.GetByTokenHash(Hash(rawToken));
            if (token != null)
            {
                _tokens.RevokeFamily(token.FamilyId);
            }
        }

        /// <summary>Signs the member out of "Remember me" on every device.</summary>
        public void RevokeAllForUser(int userId) => _tokens.RevokeAllForUser(userId);

        private static string? Truncate(string? value, int max) =>
            value == null || value.Length <= max ? value : value[..max];
    }
}
