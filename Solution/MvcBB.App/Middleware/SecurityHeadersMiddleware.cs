namespace MvcBB.App.Middleware;

/// <summary>
/// Adds the browser security headers to every response, static files included.
///
/// - X-Content-Type-Options: nosniff stops a browser from guessing that an
///   uploaded "image" is really HTML or script.
/// - frame-ancestors 'none' / X-Frame-Options: DENY stop other sites from
///   showing the forum in a hidden frame (clickjacking).
/// - The Content-Security-Policy only lets the page load scripts, styles and
///   frames from this site plus the two outside services the views use
///   (select2 from jsDelivr and Cloudflare Turnstile), blocks plugins, and
///   stops forms posting anywhere else. Images may come from anywhere, since
///   BBCode [img] tags link to outside pictures.
///
/// Inline script is still allowed because many views use inline &lt;script&gt;
/// blocks and onclick handlers. Moving those into .js files would let this
/// policy drop 'unsafe-inline' for scripts and block injected script too.
/// </summary>
public class SecurityHeadersMiddleware
{
    private const string Select2 = "https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/";
    private const string Turnstile = "https://challenges.cloudflare.com";

    private readonly RequestDelegate _next;
    private readonly string _contentSecurityPolicy;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;

        // Visual Studio's browser refresh talks to the dev server over a
        // websocket; only allow that while developing.
        var devConnect = environment.IsDevelopment() ? " ws: wss: http://localhost:* https://localhost:*" : string.Empty;

        _contentSecurityPolicy = string.Join("; ", new[]
        {
            "default-src 'self'",
            $"script-src 'self' 'unsafe-inline' {Select2} {Turnstile}",
            $"style-src 'self' 'unsafe-inline' {Select2}",
            "img-src 'self' data: https: http:",
            "font-src 'self' data:",
            $"connect-src 'self' {Turnstile}{devConnect}",
            $"frame-src {Turnstile}",
            "frame-ancestors 'none'",
            "form-action 'self'",
            "base-uri 'self'",
            "object-src 'none'"
        });
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Content-Security-Policy"] = _contentSecurityPolicy;
            return Task.CompletedTask;
        });

        return _next(context);
    }
}
