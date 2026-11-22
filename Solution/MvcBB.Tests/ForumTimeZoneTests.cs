using MvcBB.Shared.Services;

namespace MvcBB.Tests;

public class ForumTimeZoneTests
{
    // IANA ids resolve on Windows (via ICU) and Linux hosts on .NET 6+.
    private static readonly TimeZoneInfo London = ForumTimeZone.Resolve("Europe/London");
    private static readonly TimeZoneInfo NewYork = ForumTimeZone.Resolve("America/New_York");

    [Fact]
    public void Resolve_BlankId_IsServerLocalZone()
    {
        Assert.Same(TimeZoneInfo.Local, ForumTimeZone.Resolve(null));
        Assert.Same(TimeZoneInfo.Local, ForumTimeZone.Resolve(""));
        Assert.Same(TimeZoneInfo.Local, ForumTimeZone.Resolve("   "));
    }

    [Fact]
    public void Resolve_UnknownId_FallsBackToServerLocalZone()
    {
        Assert.Same(TimeZoneInfo.Local, ForumTimeZone.Resolve("Not/AZone"));
    }

    [Fact]
    public void Resolve_IanaAndWindowsIds_AreTheSameZone()
    {
        var fromWindowsId = ForumTimeZone.Resolve("GMT Standard Time");
        var summer = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(London.GetUtcOffset(summer), fromWindowsId.GetUtcOffset(summer));
        Assert.Equal(TimeSpan.FromHours(1), London.GetUtcOffset(summer));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Europe/London", true)]
    [InlineData("GMT Standard Time", true)]
    [InlineData("UTC", true)]
    [InlineData("Not/AZone", false)]
    public void IsValidId(string? id, bool expected)
    {
        Assert.Equal(expected, ForumTimeZone.IsValidId(id));
    }

    [Fact]
    public void IsValidId_RejectsOverlongIds()
    {
        Assert.False(ForumTimeZone.IsValidId(new string('a', ForumTimeZone.MaxIdLength + 1)));
    }

    [Fact]
    public void NormalizeId_PrefersIana()
    {
        Assert.Equal("Europe/London", ForumTimeZone.NormalizeId("GMT Standard Time"));
        Assert.Equal("Europe/London", ForumTimeZone.NormalizeId(" Europe/London "));
        Assert.Equal(string.Empty, ForumTimeZone.NormalizeId(null));
        Assert.Equal(string.Empty, ForumTimeZone.NormalizeId("  "));
    }

    [Fact]
    public void ToForumTime_AppliesDaylightSaving()
    {
        var winter = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var summer = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 1, 15, 12, 0, 0), ForumTimeZone.ToForumTime(winter, London));
        Assert.Equal(new DateTime(2026, 7, 15, 13, 0, 0), ForumTimeZone.ToForumTime(summer, London));
        Assert.Equal(new DateTime(2026, 1, 15, 7, 0, 0), ForumTimeZone.ToForumTime(winter, NewYork));
        Assert.Equal(new DateTime(2026, 7, 15, 8, 0, 0), ForumTimeZone.ToForumTime(summer, NewYork));
    }

    [Fact]
    public void ToForumTime_ResultIsUnspecifiedKind()
    {
        var result = ForumTimeZone.ToForumTime(DateTime.UtcNow, London);
        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
    }

    [Fact]
    public void ToForumTime_TreatsUnspecifiedAsUtc()
    {
        // What SQL Server DATETIME2 columns, and JSON without an offset, come back as.
        var unspecified = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal(new DateTime(2026, 7, 15, 13, 0, 0), ForumTimeZone.ToForumTime(unspecified, London));
    }

    [Fact]
    public void ToForumTime_ConvertsLocalKindToUtcFirst()
    {
        var utc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        var local = utc.ToLocalTime();
        Assert.Equal(ForumTimeZone.ToForumTime(utc, NewYork), ForumTimeZone.ToForumTime(local, NewYork));
    }

    [Fact]
    public void ToUtc_RoundTripsWithToForumTime()
    {
        var utc = new DateTime(2026, 10, 3, 18, 45, 0, DateTimeKind.Utc);
        var forum = ForumTimeZone.ToForumTime(utc, NewYork);
        var back = ForumTimeZone.ToUtc(forum, NewYork);

        Assert.Equal(utc, back);
        Assert.Equal(DateTimeKind.Utc, back.Kind);
    }

    [Fact]
    public void ToUtc_LeavesUtcKindUnchanged()
    {
        var utc = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        Assert.Equal(utc, ForumTimeZone.ToUtc(utc, NewYork));
    }

    [Fact]
    public void ToUtc_SkippedHour_MovesPastTheGap()
    {
        // UK clocks go forward at 01:00 on 29 March 2026: 01:30 doesn't exist.
        var skipped = new DateTime(2026, 3, 29, 1, 30, 0);
        Assert.True(London.IsInvalidTime(skipped));

        var utc = ForumTimeZone.ToUtc(skipped, London);

        // Moved to 02:00 BST, which is 01:00 UTC.
        Assert.Equal(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void ToUtc_AmbiguousHour_IsReadAsStandardTime()
    {
        // UK clocks go back at 02:00 BST on 25 October 2026: 01:30 happens twice.
        var ambiguous = new DateTime(2026, 10, 25, 1, 30, 0);
        Assert.True(London.IsAmbiguousTime(ambiguous));

        Assert.Equal(new DateTime(2026, 10, 25, 1, 30, 0, DateTimeKind.Utc), ForumTimeZone.ToUtc(ambiguous, London));
    }

    [Fact]
    public void GetOptions_IsNonEmptyWithUniqueIds()
    {
        var options = ForumTimeZone.GetOptions();

        Assert.NotEmpty(options);
        Assert.Equal(options.Count, options.Select(o => o.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(options, o => o.Id == "Europe/London");
        Assert.All(options, o => Assert.True(ForumTimeZone.IsValidId(o.Id), o.Id));
    }
}
