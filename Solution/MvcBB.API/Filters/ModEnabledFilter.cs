using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using MvcBB.ModKit;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.Filters
{
    /// <summary>
    /// Soft-disable enforcement for mod controllers. A core controller never
    /// carries [Mod] and is never touched by this filter. A mod controller
    /// tagged [Mod("id")] 404s whenever that mod's IsEnabled flag is off -
    /// no assembly unloading, no restart required for a toggle to take effect
    /// (within the cache TTL below).
    /// </summary>
    public class ModEnabledFilter : IAsyncActionFilter
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

        private readonly IModRepository _modRepository;
        private readonly IMemoryCache _cache;

        public ModEnabledFilter(IModRepository modRepository, IMemoryCache cache)
        {
            _modRepository = modRepository;
            _cache = cache;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var modAttribute = context.Controller.GetType().GetCustomAttributes(typeof(ModAttribute), inherit: true)
                .OfType<ModAttribute>().FirstOrDefault();

            if (modAttribute != null && !GetEnabledModIds().Contains(modAttribute.ModId))
            {
                context.Result = new NotFoundResult();
                return;
            }

            await next();
        }

        private HashSet<string> GetEnabledModIds()
        {
            if (_cache.TryGetValue(ModCacheKeys.EnabledModIds, out HashSet<string>? cached) && cached != null)
                return cached;

            var enabledIds = _modRepository.GetAll().Where(m => m.IsEnabled).Select(m => m.Id).ToHashSet();
            _cache.Set(ModCacheKeys.EnabledModIds, enabledIds, CacheDuration);
            return enabledIds;
        }
    }
}
