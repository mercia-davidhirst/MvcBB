namespace MvcBB.App.Exceptions;

/// <summary>
/// Thrown when the signed-in user's session cookie doesn't carry the per-user
/// API token a call needs (e.g. a session issued before that claim existed).
/// Distinct from ServiceException so callers can redirect to login instead of
/// showing a generic "please try again" error.
/// </summary>
public class SessionExpiredException : Exception
{
    public SessionExpiredException()
        : base("Your session is missing required information. Please log in again.") { }

    public SessionExpiredException(string message) : base(message) { }
}
