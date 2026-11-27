using MvcBB.API.InMemory;
using MvcBB.API.Services;
using MvcBB.App.Services;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.Tests;

public class OnlineVisitorTests
{
    private static readonly DateTime Now = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    private const string Chrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
    private const string Firefox = "Mozilla/5.0 (X11; Linux x86_64; rv:133.0) Gecko/20100101 Firefox/133.0";
    private const string Safari = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1";

    [Theory]
    [InlineData(Chrome)]
    [InlineData(Firefox)]
    [InlineData(Safari)]
    public void Browsers_AreNotBots(string userAgent)
    {
        Assert.False(BotDetector.IsBot(userAgent, out _));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", "Google")]
    [InlineData("Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm) Chrome/116.0 Safari/537.36", "Bing")]
    [InlineData("Mozilla/5.0 (compatible; AhrefsBot/7.0; +http://ahrefs.com/robot/)", "Ahrefs")]
    [InlineData("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)", "Facebook")]
    [InlineData("Mozilla/5.0 (compatible; ShinyNewBot/3.2; +https://example.com/bot)", "ShinyNewBot")]
    [InlineData("curl/8.4.0", "Other")]
    [InlineData("", "Unknown")]
    [InlineData(null, "Unknown")]
    public void Crawlers_AndScripts_AreBots_WithAName(string? userAgent, string expectedName)
    {
        Assert.True(BotDetector.IsBot(userAgent, out var name));
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void Snapshot_CountsEachKind_Once()
    {
        var tracker = new OnlineVisitorTracker();

        tracker.RecordMember("1", null, Now);
        tracker.RecordMember("1", null, Now.AddMinutes(1)); // same member, second page
        tracker.RecordMember("2", null, Now);
        tracker.RecordGuest("guestA", Now);
        tracker.RecordGuest("guestB", Now);
        tracker.RecordGuest("guestB", Now);
        tracker.RecordBot("Google", "66.249.66.1", Now);
        tracker.RecordBot("Google", "66.249.66.2", Now);
        tracker.RecordBot("Bing", "157.55.39.1", Now);

        var snapshot = tracker.GetSnapshot(15, Now.AddMinutes(1));

        Assert.Equal(2, snapshot.Members);
        Assert.Equal(2, snapshot.Guests);
        Assert.Equal(3, snapshot.Bots);
        Assert.Equal(7, snapshot.Total);
        Assert.Equal(new[] { new KeyValuePair<string, int>("Google", 2), new KeyValuePair<string, int>("Bing", 1) }, snapshot.BotNames);
    }

    [Fact]
    public void Snapshot_LeavesOut_VisitorsOlderThanTheWindow()
    {
        var tracker = new OnlineVisitorTracker();
        tracker.RecordGuest("old", Now.AddMinutes(-20));
        tracker.RecordGuest("recent", Now.AddMinutes(-4));

        Assert.Equal(1, tracker.GetSnapshot(15, Now).Guests);
        Assert.Equal(0, tracker.GetSnapshot(3, Now).Guests);
        Assert.Equal(2, tracker.GetSnapshot(30, Now).Guests);
    }

    [Fact]
    public void SigningIn_StopsCountingTheSameBrowserAsAGuest()
    {
        var tracker = new OnlineVisitorTracker();
        tracker.RecordGuest("browser1", Now);

        tracker.RecordMember("7", "browser1", Now);

        var snapshot = tracker.GetSnapshot(15, Now);
        Assert.Equal(1, snapshot.Members);
        Assert.Equal(0, snapshot.Guests);
    }

    [Fact]
    public void SigningOut_StopsCountingTheMember()
    {
        var tracker = new OnlineVisitorTracker();
        tracker.RecordMember("7", null, Now);

        tracker.RemoveMember("7");

        Assert.Equal(0, tracker.GetSnapshot(15, Now).Members);
    }

    [Fact]
    public void OldEntries_ArePruned()
    {
        var tracker = new OnlineVisitorTracker();
        tracker.RecordGuest("old", Now);

        tracker.RecordGuest("new", Now.Add(OnlineVisitorTracker.MaxWindow).AddMinutes(5));

        Assert.Equal(1, tracker.Count);
    }

    [Fact]
    public void Settings_DefaultToOn_And15Minutes()
    {
        var settings = new DisplaySettings();
        Assert.True(settings.ShowWhosOnline);
        Assert.Equal(15, settings.OnlineWindowMinutes);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(30, 30)]
    [InlineData(500, 60)]
    public void Update_ClampsTheOnlineWindow(int given, int expected)
    {
        var repository = new InMemoryDisplaySettingsRepository(new InMemoryStore());
        var service = new DisplaySettingsService(repository);

        service.UpdateDisplaySettings(new DisplaySettings { OnlineWindowMinutes = given });

        Assert.Equal(expected, repository.Get().OnlineWindowMinutes);
    }
}
