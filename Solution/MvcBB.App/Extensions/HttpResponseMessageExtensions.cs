using System.Net;
using MvcBB.App.Exceptions;

namespace MvcBB.App.Extensions;

public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Same as EnsureSuccessStatusCode(), except a 401 is treated as an expired
    /// session rather than a generic failure. The signed-in user's api_token
    /// claim can be present but no longer accepted by the API (it's a JWT with
    /// its own expiry, separate from the browser's cookie session, which can
    /// easily outlive it) - that case needs the same SessionExpiredException/
    /// RedirectToSessionExpired() handling already used when the claim is
    /// missing entirely, not a confusing "failed, try again" message.
    /// </summary>
    public static void EnsureSuccessOrSessionExpired(this HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new SessionExpiredException();
        }

        response.EnsureSuccessStatusCode();
    }
}
