using Microsoft.AspNetCore.Http;

namespace MvcBB.App.Extensions;

public static class HttpContextAccessorExtensions
{
    /// <summary>
    /// The signed-in visitor's own API JWT, or null for an anonymous visitor.
    /// Unlike the various GetCurrentUserToken() helpers used for mutating calls
    /// (which throw SessionExpiredException when absent), this is for GET calls
    /// that must still work for anonymous visitors - the API uses the token's
    /// presence to decide whether to include private-board content.
    /// </summary>
    public static string? GetApiTokenOrNull(this IHttpContextAccessor accessor)
        => accessor.HttpContext?.User.FindFirst("api_token")?.Value;
}
