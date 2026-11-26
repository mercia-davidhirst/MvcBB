using System.Net;
using System.Text.Json;
using MvcBB.App.Exceptions;

namespace MvcBB.App.Extensions;

public static class ApiErrorExtensions
{
    /// <summary>
    /// Like EnsureSuccessOrSessionExpired, but a failure the API explained
    /// with a { "message": "..." } body (or ValidationProblemDetails errors)
    /// becomes a ServiceException carrying that text, so the page can show
    /// the member why instead of a generic failure.
    /// </summary>
    public static async Task EnsureSuccessWithMessageAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new SessionExpiredException();
        }

        var body = await response.Content.ReadAsStringAsync();
        string? message = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                {
                    message = m.GetString();
                }
                else if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    message = string.Join(" ", errors.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                        .Where(s => !string.IsNullOrEmpty(s)));
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON - fall through to the generic message.
        }

        throw new ServiceException(string.IsNullOrWhiteSpace(message)
            ? $"The request failed ({(int)response.StatusCode} {response.ReasonPhrase})."
            : message!);
    }
}
