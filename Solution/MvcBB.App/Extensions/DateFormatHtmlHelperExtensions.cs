using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.App.Extensions;

/// <summary>
/// Formats dates/times using the site's configured DisplaySettings.DateFormat/
/// TimeFormat (Admin &gt; Settings) instead of a hardcoded pattern, so every
/// page that shows a timestamp honours the same admin-configurable format.
/// The settings are fetched once per request (cached on HttpContext.Items)
/// rather than once per date rendered, since a single page can show dozens
/// of them (e.g. a thread's posts).
/// </summary>
public static class DateFormatHtmlHelperExtensions
{
    private const string CacheKey = "MvcBB.App.DisplaySettingsForDateFormat";

    /// <summary>Formats a UTC date+time in the visitor's local time zone, e.g. "14/11/2026 16:30".</summary>
    public static async Task<string> FormatDateTimeAsync(this IHtmlHelper html, DateTime utcDateTime)
    {
        var settings = await GetSettingsAsync(html);
        return utcDateTime.ToLocalTime().ToString($"{settings.DateFormat} {settings.TimeFormat}");
    }

    /// <summary>Formats a UTC date (date portion only) in the visitor's local time zone, e.g. "14/11/2026".</summary>
    public static async Task<string> FormatDateAsync(this IHtmlHelper html, DateTime utcDateTime)
    {
        var settings = await GetSettingsAsync(html);
        return utcDateTime.ToLocalTime().ToString(settings.DateFormat);
    }

    private static async Task<DisplaySettings> GetSettingsAsync(IHtmlHelper html)
    {
        var context = html.ViewContext.HttpContext;
        if (context.Items[CacheKey] is DisplaySettings cached)
        {
            return cached;
        }

        var settingsService = context.RequestServices.GetRequiredService<ISettingsService>();
        var settings = await settingsService.GetDisplaySettingsAsync();
        context.Items[CacheKey] = settings;
        return settings;
    }
}
