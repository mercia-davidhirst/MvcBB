using MvcBB.API.InMemory;
using MvcBB.API.Services;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.Tests;

public class DisplaySettingsServiceTests
{
    private static (DisplaySettingsService Service, InMemoryDisplaySettingsRepository Repository) Create()
    {
        var repository = new InMemoryDisplaySettingsRepository(new InMemoryStore());
        return (new DisplaySettingsService(repository), repository);
    }

    [Fact]
    public void DefaultTimeZone_IsBlank_ServerLocalTime()
    {
        Assert.Equal(string.Empty, new DisplaySettings().TimeZoneId);
    }

    [Fact]
    public void Update_StoresWindowsIdAsIana()
    {
        var (service, repository) = Create();

        service.UpdateDisplaySettings(new DisplaySettings { TimeZoneId = "GMT Standard Time" });

        Assert.Equal("Europe/London", repository.Get().TimeZoneId);
    }

    [Fact]
    public void Update_KeepsCurrentZone_WhenGivenAnUnknownId()
    {
        var (service, repository) = Create();
        service.UpdateDisplaySettings(new DisplaySettings { TimeZoneId = "America/New_York" });

        service.UpdateDisplaySettings(new DisplaySettings { TimeZoneId = "Not/AZone" });

        Assert.Equal("America/New_York", repository.Get().TimeZoneId);
    }

    [Fact]
    public void Update_AllowsClearingBackToServerLocalTime()
    {
        var (service, repository) = Create();
        service.UpdateDisplaySettings(new DisplaySettings { TimeZoneId = "America/New_York" });

        service.UpdateDisplaySettings(new DisplaySettings { TimeZoneId = "" });

        Assert.Equal(string.Empty, repository.Get().TimeZoneId);
    }
}
