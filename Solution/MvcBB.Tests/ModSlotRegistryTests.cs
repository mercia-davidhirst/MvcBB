using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MvcBB.App.Mods;
using MvcBB.ModKit;

namespace MvcBB.Tests;

public class ModSlotRegistryTests
{
    [Theory]
    [InlineData(CoreSlots.HomeSidebar, "home.sidebar")]
    [InlineData(CoreSlots.HomeLeft, "home.left")]
    [InlineData(CoreSlots.HomeCenterTop, "home.center.top")]
    [InlineData(CoreSlots.HomeCenter, "home.center")]
    [InlineData(CoreSlots.HomeRight, "home.right")]
    public void HomeSlots_AreKnownBlockSlotsWithPageContext(string slot, string expectedName)
    {
        Assert.Equal(expectedName, slot);
        var definition = ModSlotRegistry.KnownSlots[slot];
        Assert.Equal(typeof(PageSlotContext), definition.ContextType);
        Assert.Equal(ModSlotRegistry.SlotWrapper.Block, definition.Wrapper);
    }

    [Fact]
    public void Build_RegistersHomeColumnContributions_AndKeepsHomeSidebar()
    {
        var registry = ModSlotRegistry.Build(new IAppMod[] { new PortalTestMod() }, NullLogger.Instance);

        Assert.True(registry.HasAny);
        Assert.Single(registry.GetEntries(CoreSlots.HomeSidebar));
        Assert.Equal(new[] { typeof(FirstWidgetViewComponent), typeof(SecondWidgetViewComponent) },
            registry.GetEntries(CoreSlots.HomeLeft).Select(e => e.ViewComponentType));
        Assert.Single(registry.GetEntries(CoreSlots.HomeCenter));
        Assert.Single(registry.GetEntries(CoreSlots.HomeCenterTop));
        Assert.Single(registry.GetEntries(CoreSlots.HomeRight));
    }

    [Fact]
    public void Build_IgnoresUnknownSlotsAndBadComponents()
    {
        var registry = ModSlotRegistry.Build(new IAppMod[] { new PortalTestMod() }, NullLogger.Instance);

        Assert.Empty(registry.GetEntries("home.nowhere"));
        Assert.DoesNotContain(registry.GetEntries(CoreSlots.HomeRight), e => e.ViewComponentType == typeof(WrongContextViewComponent));
    }

    [Fact]
    public void Build_ModWithoutSlotContributions_ContributesNothing()
    {
        var registry = ModSlotRegistry.Build(new IAppMod[] { new LegacyTestMod() }, NullLogger.Instance);

        Assert.False(registry.HasAny);
    }
}

// Top-level (not nested) because the registry requires slot view components
// to be public types, and a nested type isn't Type.IsPublic.
public sealed class PortalTestMod : IAppMod
{
    public string Id => "portaltest";
    public string Name => "Portal test";
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    public IEnumerable<ModNavItem> GetNavItems() => Array.Empty<ModNavItem>();

    public IEnumerable<ModSlotContribution> GetSlotContributions() => new[]
    {
        new ModSlotContribution(CoreSlots.HomeSidebar, typeof(FirstWidgetViewComponent)),
        // Order 10 listed before order 0, to check sorting.
        new ModSlotContribution(CoreSlots.HomeLeft, typeof(SecondWidgetViewComponent), order: 10),
        new ModSlotContribution(CoreSlots.HomeLeft, typeof(FirstWidgetViewComponent), order: 0),
        new ModSlotContribution(CoreSlots.HomeCenterTop, typeof(FirstWidgetViewComponent)),
        new ModSlotContribution(CoreSlots.HomeCenter, typeof(SecondWidgetViewComponent)),
        new ModSlotContribution(CoreSlots.HomeRight, typeof(FirstWidgetViewComponent)),
        new ModSlotContribution(CoreSlots.HomeRight, typeof(WrongContextViewComponent)),
        new ModSlotContribution("home.nowhere", typeof(FirstWidgetViewComponent)),
    };
}

/// <summary>A mod built before view slots existed: relies on the default GetSlotContributions.</summary>
public sealed class LegacyTestMod : IAppMod
{
    public string Id => "legacy";
    public string Name => "Legacy";
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    public IEnumerable<ModNavItem> GetNavItems() => Array.Empty<ModNavItem>();
}

public class FirstWidgetViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PageSlotContext context) => Content("first");
}

public class SecondWidgetViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PageSlotContext context) => Content("second");
}

public class WrongContextViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PostSlotContext context) => Content("wrong");
}
