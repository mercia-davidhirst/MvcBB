using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MvcBB.Shared.Models.User;

namespace MvcBB.App.Services
{
    public enum SessionRefreshStatus
    {
        /// <summary>New tokens issued.</summary>
        Renewed,

        /// <summary>The API refused the refresh token: the session is over.</summary>
        Ended,

        /// <summary>
        /// Couldn't tell (API unreachable, or another request is renewing the
        /// same session). Leave the session as it is for this request.
        /// </summary>
        Unavailable
    }

    public sealed record SessionRefreshResult(SessionRefreshStatus Status, UserAuthResponse? Response = null);

    /// <summary>
    /// Swaps a "Remember me" refresh token for new tokens at the API, and
    /// revokes one on sign-out.
    ///
    /// Refresh tokens are single-use, and a browser often sends several
    /// requests at once (a page and its background calls) carrying the same
    /// cookie. So renewals are serialised per token, and each result is kept
    /// for a minute: the other requests get the same new tokens instead of
    /// spending the old one again, which the API would treat as theft.
    /// </summary>
    public class SessionRefresher
    {
        private static readonly TimeSpan ResultLifetime = TimeSpan.FromSeconds(60);
        private readonly SemaphoreSlim[] _locks = Enumerable.Range(0, 32).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SessionRefresher> _logger;

        public SessionRefresher(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<SessionRefresher> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<SessionRefreshResult> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            var key = "mvcbb-session-refresh:" + Convert.ToHexString(hash);
            if (_cache.TryGetValue(key, out SessionRefreshResult? cached) && cached != null)
            {
                return cached;
            }

            var gate = _locks[hash[0] % _locks.Length];
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(key, out cached) && cached != null)
                {
                    return cached;
                }

                var result = await CallApiAsync(refreshToken, ipAddress, cancellationToken);
                if (result.Status != SessionRefreshStatus.Unavailable)
                {
                    _cache.Set(key, result, ResultLifetime);
                }
                return result;
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<SessionRefreshResult> CallApiAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MvcBBApi");
                using var response = await client.PostAsJsonAsync("api/users/refresh",
                    new RefreshTokenRequest { RefreshToken = refreshToken, IpAddress = ipAddress }, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var auth = await response.Content.ReadFromJsonAsync<UserAuthResponse>(JsonOptions, cancellationToken);
                    return auth is { Token.Length: > 0 }
                        ? new SessionRefreshResult(SessionRefreshStatus.Renewed, auth)
                        : new SessionRefreshResult(SessionRefreshStatus.Unavailable);
                }

                return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest
                    ? new SessionRefreshResult(SessionRefreshStatus.Ended)
                    : new SessionRefreshResult(SessionRefreshStatus.Unavailable);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Could not reach the API to renew a remembered session; leaving it in place for this request.");
                return new SessionRefreshResult(SessionRefreshStatus.Unavailable);
            }
        }

        /// <summary>
        /// Revokes the device's refresh tokens at the API. Best effort: signing
        /// out of the App goes ahead even if the API can't be reached.
        /// </summary>
        public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MvcBBApi");
                using var response = await client.PostAsJsonAsync("api/users/logout",
                    new RefreshTokenRequest { RefreshToken = refreshToken }, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Could not reach the API to revoke a remembered session on sign-out.");
            }
        }
    }
}
