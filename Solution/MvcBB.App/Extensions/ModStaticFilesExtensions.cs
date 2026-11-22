using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using MvcBB.App.Interfaces;
using MvcBB.ModKit;

namespace MvcBB.App.Extensions;

/// <summary>
/// Serves static files (CSS, images, scripts) that a mod embeds in its own
/// App-side DLL, at /_mods/{modId}/{path}. This exists because a Razor Class
/// Library's usual wwwroot handling (/_content/{Assembly}/...) is wired up by
/// the host's build, so it never applies to a mod DLL dropped into Mods/ at
/// runtime.
///
/// A mod opts in by embedding its wwwroot folder with a manifest (see
/// docs/ModKit-ViewSlots.md); a mod without a manifest simply ships no static
/// files and is skipped. Requests for a disabled mod's files 404, the same as
/// its pages (see Filters/ModEnabledFilter.cs).
///
/// The "_mods" prefix keeps these URLs clear of the admin Mods page
/// (/Mods/...), which routing would otherwise match case-insensitively.
/// </summary>
public static class ModStaticFilesExtensions
{
    public const string RequestPathPrefix = "/_mods";

    /// <summary>Folder inside the mod's project whose contents are served.</summary>
    public const string EmbeddedRoot = "wwwroot";

    private static readonly Regex SafeModId = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    public static WebApplication UseModStaticFiles(this WebApplication app, IEnumerable<IAppMod> appMods)
    {
        var servedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in appMods)
        {
            var modId = mod.Id;
            if (string.IsNullOrWhiteSpace(modId) || !SafeModId.IsMatch(modId))
            {
                app.Logger.LogWarning("Mod static files: skipped mod {ModName}, its Id \"{ModId}\" isn't URL-safe.", mod.Name, modId);
                continue;
            }
            if (!servedIds.Add(modId))
            {
                app.Logger.LogWarning("Mod static files: skipped a second mod with Id \"{ModId}\".", modId);
                continue;
            }

            ManifestEmbeddedFileProvider fileProvider;
            try
            {
                fileProvider = new ManifestEmbeddedFileProvider(mod.GetType().Assembly, EmbeddedRoot);
            }
            catch (InvalidOperationException)
            {
                // No embedded files manifest (or no wwwroot in it) - this mod
                // ships no static files, which is the normal case.
                continue;
            }

            var requestPath = new PathString($"{RequestPathPrefix}/{modId}");
            app.UseWhen(context => context.Request.Path.StartsWithSegments(requestPath), branch =>
            {
                branch.Use(async (context, next) =>
                {
                    if (!await IsModEnabledAsync(context, modId))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    await next();
                });
                branch.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = fileProvider,
                    RequestPath = requestPath
                });
            });

            app.Logger.LogInformation("Mod static files: serving mod \"{ModId}\" at {RequestPath}.", modId, requestPath);
        }

        return app;
    }

    private static async Task<bool> IsModEnabledAsync(HttpContext context, string modId)
    {
        try
        {
            var modsService = context.RequestServices.GetRequiredService<IModsService>();
            var enabledIds = await modsService.GetEnabledModIdsAsync();
            return enabledIds.Contains(modId);
        }
        catch (Exception ex)
        {
            // API unreachable: treat as disabled rather than failing the request.
            context.RequestServices.GetRequiredService<ILogger<IAppMod>>()
                .LogWarning(ex, "Mod static files: couldn't check whether mod \"{ModId}\" is enabled.", modId);
            return false;
        }
    }
}
