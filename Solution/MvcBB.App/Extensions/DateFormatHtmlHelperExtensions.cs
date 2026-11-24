using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Settings;
using MvcBB.Shared.Services;

namespace MvcBB.App.Extensions;

/// <summary>
/// The single place MvcBB.App turns stored timestamps into what visitors see.
/// Every timestamp is stored in UTC; these helpers convert it to the forum's
/// time zone (DisplaySettings.TimeZoneId, Admin &gt; Settings) and format it
/// with the site's DateFormat/TimeFormat, so every page agrees.
/// The settings are fetched once per request (cached on HttpContext.Items)
/// rather than once per date rendered, since a single page can show dozens
/// of them (e.g. a thread's posts).
/// </summary>
public static class DateFormatHtmlHelperExtensions
{
    private const string CacheKey = "MvcBB.App.DisplaySettingsForDateFormat";

    /// <summary>Formats a UTC date+time in the forum's time zone, e.g. "14/11/2026 16:30".</summary>
    public static async Task<string> FormatDateTimeAsync(this IHtmlHelper html, DateTime utcDateTime)
    {
        var settings = await html.ViewContext.HttpContext.GetDisplaySettingsForDatesAsync();
        return ToForumTime(utcDateTime, settings).ToString($"{settings.DateFormat} {settings.TimeFormat}");
    }

    /// <summary>Formats a UTC date (date portion only) in the forum's time zone, e.g. "14/11/2026".</summary>
    public static async Task<string> FormatDateAsync(this IHtmlHelper html, DateTime utcDateTime)
    {
        var settings = await html.ViewContext.HttpContext.GetDisplaySettingsForDatesAsync();
        return ToForumTime(utcDateTime, settings).ToString(settings.DateFormat);
    }

    /// <summary>Formats a UTC time (time portion only) in the forum's time zone, e.g. "16:30".</summary>
    public static async Task<string> FormatTimeAsync(this IHtmlHelper html, DateTime utcDateTime)
    {
        var settings = await html.ViewContext.HttpContext.GetDisplaySettingsForDatesAsync();
        return ToForumTime(utcDateTime, settings).ToString(settings.TimeFormat);
    }

    /// <summary>
    /// A UTC timestamp converted to forum wall-clock time, for views that need
    /// their own format string. Prefer <see cref="FormatDateTimeAsync"/>.
    /// </summary>
    public static async Task<DateTime> ToForumTimeAsync(this IHtmlHelper html, DateTime utcDateTime)
    {
        var settings = await html.ViewContext.HttpContext.GetDisplaySettingsForDatesAsync();
        return ToForumTime(utcDateTime, settings);
    }

    /// <summary>The current forum wall-clock time.</summary>
    public static Task<DateTime> ForumNowAsync(this IHtmlHelper html) => html.ToForumTimeAsync(DateTime.UtcNow);

    /// <summary>The forum's time zone, for controllers converting admin-entered dates.</summary>
    public static async Task<TimeZoneInfo> GetForumTimeZoneAsync(this HttpContext context)
    {
        var settings = await context.GetDisplaySettingsForDatesAsync();
        return ForumTimeZone.Resolve(settings.TimeZoneId);
    }

    /// <summary>
    /// Converts a wall-clock time an admin typed (forum time zone) to UTC for
    /// storage. Null stays null.
    /// </summary>
    public static async Task<DateTime?> ForumTimeToUtcAsync(this HttpContext context, DateTime? forumDateTime)
    {
        if (!forumDateTime.HasValue)
        {
            return null;
        }

        return ForumTimeZone.ToUtc(forumDateTime.Value, await context.GetForumTimeZoneAsync());
    }

    /// <summary>
    /// Converts a stored UTC time to forum wall-clock time, e.g. to pre-fill a
    /// datetime-local input. Null stays null.
    /// </summary>
    public static async Task<DateTime?> UtcToForumTimeAsync(this HttpContext context, DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue)
        {
            return null;
        }

        return ForumTimeZone.ToForumTime(utcDateTime.Value, await context.GetForumTimeZoneAsync());
    }

    private static DateTime ToForumTime(DateTime utcDateTime, DisplaySettings settings) =>
        ForumTimeZone.ToForumTime(utcDateTime, ForumTimeZone.Resolve(settings.TimeZoneId));

    private static async Task<DisplaySettings> GetDisplaySettingsForDatesAsync(this HttpContext context)
    {
        if (context.Items[CacheKey] is DisplaySettings cached)
        {
            return cached;
        }

        var settingsService = context.RequestServices.GetRequiredService<ISettingsService>();
        DisplaySettings settings;
        try
        {
            settings = await settingsService.GetDisplaySettingsAsync();
        }
        catch
        {
            // The API being briefly unreachable shouldn't break every date on
            // the page: fall back to the defaults (server local time).
            settings = new DisplaySettings();
        }

        context.Items[CacheKey] = settings;
        return settings;
    }
}
