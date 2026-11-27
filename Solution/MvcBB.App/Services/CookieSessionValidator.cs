using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Checks a signed-in member's cookie against the API on every request, so
    /// a session ends as soon as the member is locked, demoted or changes their
    /// password (anywhere), instead of lasting until the cookie expires.
    ///
    /// The cookie carries the member's API token. The API refuses that token
    /// once the account's password, role or lock has changed since it was
    /// issued (see JwtTokenService.CheckUserSession in MvcBB.API), so asking
    /// the API "is this token still good?" answers "is this session still
    /// good?". If it isn't, the cookie is thrown away and the member is signed
    /// out.
    ///
    /// A "Remember me" session also carries a refresh token. When its API
    /// token is about to run out (or has been refused), the refresh token is
    /// swapped for a new pair and the cookie re-issued, so the member stays
    /// signed in. The API makes the same checks before renewing, so a lock,
    /// demotion, password change or sign-out still ends a remembered session
    /// straight away.
    ///
    /// If the API can't be reached at all, the session is left alone for that
    /// request: every API call the page makes with the token is still checked,
    /// and signing everyone out during an API restart would be worse.
    /// </summary>
    public static class CookieSessionValidator
    {
        /// <summary>Renew a remembered session this long before its API token runs out.</summary>
        private static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(5);

        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            if (context.Principal?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var token = context.Principal.FindFirst(SessionCookie.ApiTokenClaim)?.Value;
            if (string.IsNullOrEmpty(token))
            {
                await RejectAsync(context);
                return;
            }

            var refreshToken = SessionCookie.GetRefreshToken(context.Principal);
            if (refreshToken != null
                && SessionCookie.GetApiTokenExpiry(context.Principal) is { } expiresAt
                && expiresAt - DateTime.UtcNow < RenewBefore)
            {
                switch (await RenewAsync(context, refreshToken))
                {
                    case SessionRefreshStatus.Renewed:
                        // The API just checked the account to issue these.
                        return;
                    case SessionRefreshStatus.Ended:
                        await RejectAsync(context);
                        return;
                    // Unavailable: carry on with the token we have.
                }
            }

            var services = context.HttpContext.RequestServices;
            var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("MvcBBApi");

            HttpResponseMessage response;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/users/session");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request, context.HttpContext.RequestAborted);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                services.GetRequiredService<ILogger<CookieSessionMarker>>()
                    .LogWarning(ex, "Could not reach the API to check a session; leaving it in place for this request.");
                return;
            }

            using (response)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    // A remembered session gets one go at renewing (its token may
                    // simply have expired); the API refuses if the session was
                    // ended for any other reason.
                    var status = refreshToken != null
                        ? await RenewAsync(context, refreshToken)
                        : SessionRefreshStatus.Ended;
                    if (status == SessionRefreshStatus.Ended)
                    {
                        await RejectAsync(context);
                    }
                }
            }
        }

        private static async Task<SessionRefreshStatus> RenewAsync(CookieValidatePrincipalContext context, string refreshToken)
        {
            var refresher = context.HttpContext.RequestServices.GetRequiredService<SessionRefresher>();
            var result = await refresher.RefreshAsync(
                refreshToken,
                context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                context.HttpContext.RequestAborted);

            if (result.Status == SessionRefreshStatus.Renewed && result.Response != null)
            {
                var tokens = SessionTokens.From(result.Response);
                context.ReplacePrincipal(SessionCookie.CreatePrincipal(result.Response));
                SessionCookie.ApplyExpiry(context.Properties, tokens);
                context.ShouldRenew = true;
            }

            return result.Status;
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // Only used as the logger category.
        public sealed class CookieSessionMarker { }
    }
}
