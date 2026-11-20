using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MvcBB.App.Interfaces;
using MvcBB.ModKit;

namespace MvcBB.App.Filters
{
    /// <summary>
    /// App-side half of the soft-disable mechanism (see MvcBB.API.Filters.ModEnabledFilter
    /// for the API-side counterpart). A core controller never carries [Mod] and
    /// is never touched here.
    /// </summary>
    public class ModEnabledFilter : IAsyncActionFilter
    {
        private readonly IModsService _modsService;

        public ModEnabledFilter(IModsService modsService)
        {
            _modsService = modsService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var modAttribute = context.Controller.GetType().GetCustomAttributes(typeof(ModAttribute), inherit: true)
                .OfType<ModAttribute>().FirstOrDefault();

            if (modAttribute != null)
            {
                var enabledIds = await _modsService.GetEnabledModIdsAsync();
                if (!enabledIds.Contains(modAttribute.ModId))
                {
                    context.Result = new NotFoundResult();
                    return;
                }
            }

            await next();
        }
    }
}
