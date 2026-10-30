using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MvcBB.App.Extensions;

public static class ControllerExtensions
{
    /// <summary>
    /// Signs out the current (incomplete) session and sends the user back to
    /// login with a message explaining why, plus a returnUrl back to whatever
    /// page they were on. Signing out first matters: the stale cookie is still
    /// technically "authenticated" (just missing a claim added after it was
    /// issued), so a plain redirect to the Login page would just bounce them
    /// straight back out via its own "already logged in" check.
    /// </summary>
    public static async Task<IActionResult> RedirectToSessionExpired(this Controller controller)
    {
        await controller.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        controller.TempData["Error"] = "Your session has expired. Please log in again.";
        var returnUrl = controller.Request.Path + controller.Request.QueryString;
        return controller.RedirectToAction("Login", "Account", new { returnUrl });
    }
}
