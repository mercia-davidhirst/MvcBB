namespace MvcBB.App.Middleware;

/// <summary>
/// Forces a redirect to /Account/ChangePasswordRequired for any authenticated
/// request carrying the "must_change_password" claim (set at login - see
/// AccountController.SignInUserAsync), until the user actually changes their
/// password (which re-signs them in without the claim - see
/// AccountController.ChangePasswordRequired POST).
/// </summary>
public class RequirePasswordChangeMiddleware
{
    private const string ChangePasswordPath = "/Account/ChangePasswordRequired";

    private static readonly string[] AllowedPathPrefixes =
    {
        ChangePasswordPath,
        "/Account/Logout",
        "/css", "/js", "/lib", "/images", "/favicon"
    };

    private readonly RequestDelegate _next;

    public RequirePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var mustChangePassword = context.User.Identity?.IsAuthenticated == true
            && context.User.FindFirst("must_change_password") != null;

        if (mustChangePassword && !IsAllowedPath(context.Request.Path))
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"{ChangePasswordPath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        await _next(context);
    }

    private static bool IsAllowedPath(PathString path) =>
        AllowedPathPrefixes.Any(prefix => path.StartsWithSegments(prefix) || path.Value?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
}
