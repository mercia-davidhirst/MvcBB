using System.Net;
using MvcBB.App.Exceptions;

namespace MvcBB.App.Middleware;

/// <summary>
/// Middleware for handling exceptions and returning appropriate error responses
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");

            // Too late to replace the response - let the server abort it.
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/json";

        context.Response.StatusCode = exception switch
        {
            ServiceException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };

        // Never send the raw exception text or type to the browser - it can
        // reveal file paths, SQL, internal URLs or stack details. The full
        // exception is in the log above. ServiceException messages are the
        // exception: they're written by this app for users to read.
        var message = exception switch
        {
            ServiceException => exception.Message,
            UnauthorizedAccessException => "You are not allowed to do that.",
            KeyNotFoundException => "The requested item was not found.",
            _ => "Something went wrong. Please try again later."
        };

        var response = new
        {
            error = new
            {
                message
            }
        };

        await context.Response.WriteAsJsonAsync(response);
    }
} 