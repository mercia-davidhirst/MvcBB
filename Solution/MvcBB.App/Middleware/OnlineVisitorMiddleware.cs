using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MvcBB.App.Services;

namespace MvcBB.App.Middleware;

/// <summary>
/// Notes every page request in <see cref="OnlineVisitorTracker"/> as a
/// member, guest or bot, for the home page "Who's online" card. Runs after
/// authentication (so signed-in members are known) and after static files
/// (which never reach it). It only records; it never changes the request.
/// </summary>
public class OnlineVisitorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly OnlineVisitorTracker _tracker;

    public OnlineVisitorMiddleware(RequestDelegate next, OnlineVisitorTracker tracker)
    {
        _next = next;
        _tracker = tracker;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            Record(context);
        }
        catch
        {
            // Counting visitors must never break a page.
        }

        await _next(context);
    }

    private void Record(HttpContext context)
    {
        var method = context.Request.Method;
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsPost(method))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var ip = context.Connection.RemoteIpAddress?.ToString();

        var userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

        if (!string.IsNullOrEmpty(userId))
        {
            _tracker.RecordMember(userId, GuestKey(ip, userAgent), now);
        }
        else if (BotDetector.IsBot(userAgent, out var botName))
        {
            _tracker.RecordBot(botName, ip, now);
        }
        else
        {
            _tracker.RecordGuest(GuestKey(ip, userAgent), now);
        }
    }

    /// <summary>
    /// A guest's key: a short hash of their IP address and user agent, so the
    /// tracker never holds either one as it is and no cookie is needed.
    /// </summary>
    internal static string GuestKey(string? ip, string userAgent)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes((ip ?? string.Empty) + "|" + userAgent));
        return Convert.ToHexString(bytes, 0, 12);
    }
}
