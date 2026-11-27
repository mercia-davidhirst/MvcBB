using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MvcBB.Shared.Models.User;

namespace MvcBB.App.Services
{
    /// <summary>The API tokens a signed-in session carries in its cookie.</summary>
    public sealed record SessionTokens(string Token, DateTime ExpiresAt, string? RefreshToken, DateTime? RefreshTokenExpiresAt)
    {
        public static SessionTokens From(UserAuthResponse response) =>
            new(response.Token, response.ExpiresAt, response.RefreshToken, response.RefreshTokenExpiresAt);
    }

    /// <summary>
    /// Builds the App's sign-in cookie, so signing in, renewing and changing
    /// password all shape it the same way.
    ///
    /// - Without "Remember me" it's a browser-session cookie that lasts as
    ///   long as the API token inside it (Jwt:TokenExpirationHours).
    /// - With "Remember me" it's a persistent cookie that lasts as long as
    ///   the refresh token inside it (Jwt:RefreshTokenExpirationDays). When
    ///   the API token is about to run out, CookieSessionValidator swaps the
    ///   refresh token for a new pair and re-issues the cookie, so the member
    ///   stays signed in without noticing.
    /// </summary>
    public static class SessionCookie
    {
        public const string ApiTokenClaim = "api_token";
        public const string ApiTokenExpiresClaim = "api_token_expires";
        public const string RefreshTokenClaim = "refresh_token";
        public const string MustChangePasswordClaim = "must_change_password";

        public static ClaimsPrincipal CreatePrincipal(UserAuthResponse response)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, response.User.Id.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, response.User.Username),
                new(ClaimTypes.Email, response.User.Email),
                new(ClaimTypes.Role, response.User.RoleName)
            };

            if (response.User.MustChangePassword)
            {
                claims.Add(new Claim(MustChangePasswordClaim, "true"));
            }

            AddTokenClaims(claims, SessionTokens.From(response));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        /// <summary>
        /// The same principal with new API tokens swapped in. A null refresh
        /// token keeps the one the session already has.
        /// </summary>
        public static ClaimsPrincipal WithTokens(ClaimsPrincipal principal, SessionTokens tokens, bool removeMustChangePassword = false)
        {
            var keptRefresh = tokens.RefreshToken == null ? principal.FindFirst(RefreshTokenClaim)?.Value : null;
            var claims = principal.Claims
                .Where(c => c.Type is not (ApiTokenClaim or ApiTokenExpiresClaim or RefreshTokenClaim))
                .Where(c => !(removeMustChangePassword && c.Type == MustChangePasswordClaim))
                .Select(c => new Claim(c.Type, c.Value))
                .ToList();

            AddTokenClaims(claims, keptRefresh == null ? tokens : tokens with { RefreshToken = keptRefresh });
            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        public static AuthenticationProperties CreateProperties(SessionTokens tokens)
        {
            var properties = new AuthenticationProperties();
            ApplyExpiry(properties, tokens);
            return properties;
        }

        /// <summary>
        /// Remembered sessions persist until the refresh token runs out; others
        /// end with the browser session or the API token, whichever is first.
        /// A null refresh expiry leaves a remembered session's expiry alone.
        /// </summary>
        public static void ApplyExpiry(AuthenticationProperties properties, SessionTokens tokens)
        {
            properties.IssuedUtc = DateTimeOffset.UtcNow;
            if (tokens.RefreshTokenExpiresAt is { } refreshExpires)
            {
                properties.IsPersistent = true;
                properties.ExpiresUtc = DateTime.SpecifyKind(refreshExpires, DateTimeKind.Utc);
            }
            else if (!properties.IsPersistent)
            {
                properties.ExpiresUtc = DateTime.SpecifyKind(tokens.ExpiresAt, DateTimeKind.Utc);
            }
        }

        public static string? GetRefreshToken(ClaimsPrincipal principal) =>
            principal.FindFirst(RefreshTokenClaim)?.Value is { Length: > 0 } value ? value : null;

        /// <summary>When the session's API token runs out, if the cookie says.</summary>
        public static DateTime? GetApiTokenExpiry(ClaimsPrincipal principal) =>
            DateTime.TryParse(principal.FindFirst(ApiTokenExpiresClaim)?.Value, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var value)
                ? value.ToUniversalTime()
                : null;

        private static void AddTokenClaims(List<Claim> claims, SessionTokens tokens)
        {
            claims.Add(new Claim(ApiTokenClaim, tokens.Token));
            claims.Add(new Claim(ApiTokenExpiresClaim,
                DateTime.SpecifyKind(tokens.ExpiresAt, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture)));
            if (!string.IsNullOrEmpty(tokens.RefreshToken))
            {
                claims.Add(new Claim(RefreshTokenClaim, tokens.RefreshToken));
            }
        }
    }
}
