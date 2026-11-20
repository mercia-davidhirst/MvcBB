using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using MvcBB.App.Exceptions;
using MvcBB.App.Interfaces;
using MvcBB.ModKit;
using MvcBB.Shared.Models.Mod;

namespace MvcBB.App.Services
{
    /// <summary>Client for the mod-registry endpoints on MvcBB.API (see ModsController).</summary>
    public class ModsService : IModsService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ModsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public async Task<IEnumerable<ModRecord>> GetAllAsync()
        {
            AttachAdminToken();
            var response = await _httpClient.GetAsync("api/mods");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<ModRecord>>(content, _jsonOptions) ?? Array.Empty<ModRecord>();
        }

        public async Task SetEnabledAsync(string id, bool enabled)
        {
            AttachAdminToken();
            var response = await _httpClient.PostAsync($"api/mods/{id}/{(enabled ? "enable" : "disable")}", content: null);
            response.EnsureSuccessStatusCode();

            // The API's own cache self-invalidates on toggle, but this process's
            // locally-cached copy (used by nav rendering and the App-side
            // ModEnabledFilter) won't notice until this is cleared too.
            _cache.Remove(ModCacheKeys.EnabledModIds);
        }

        public async Task<HashSet<string>> GetEnabledModIdsAsync()
        {
            if (_cache.TryGetValue(ModCacheKeys.EnabledModIds, out HashSet<string>? cached) && cached != null)
                return cached;

            var response = await _httpClient.GetAsync("api/mods/enabled");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var enabledIds = JsonSerializer.Deserialize<HashSet<string>>(content, _jsonOptions) ?? new HashSet<string>();

            _cache.Set(ModCacheKeys.EnabledModIds, enabledIds, CacheDuration);
            return enabledIds;
        }

        private void AttachAdminToken()
        {
            var token = _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
