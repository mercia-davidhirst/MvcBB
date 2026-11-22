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
    /// If the API can't be reached at all, the session is left alone for that
    /// request: every API call the page makes with the token is still checked,
    /// and signing everyone out during an API restart would be worse.
    /// </summary>
    public static class CookieSessionValidator
    {
        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            if (context.Principal?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var token = context.Principal.FindFirst("api_token")?.Value;
            if (string.IsNullOrEmpty(token))
            {
                await RejectAsync(context);
                return;
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
                    await RejectAsync(context);
                }
            }
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
