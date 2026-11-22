using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MvcBB.API.Filters
{
    /// <summary>
    /// Makes the App's shared service token (role "ApiClient") read-only.
    /// MvcBB.App uses it only to fetch data on behalf of guests and for a few
    /// lookups; anything that changes data must be sent with the signed-in
    /// user's own token, so the API can check who is actually asking.
    ///
    /// A request carrying the service token is refused (403) unless it's a
    /// read (GET/HEAD/OPTIONS) or it targets an endpoint that's open to
    /// anonymous callers anyway (login, register, password reset, ...), which
    /// the App may call on the same HttpClient after a lookup.
    ///
    /// Runs as a global filter, so it covers mod controllers too.
    /// </summary>
    public class ServiceTokenReadOnlyFilter : IAuthorizationFilter
    {
        public const string ServiceRole = "ApiClient";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true || !user.IsInRole(ServiceRole))
            {
                return;
            }

            var method = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
            {
                return;
            }

            if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            context.Result = new ObjectResult(new { message = "The service token is read-only. Sign in to make changes." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
