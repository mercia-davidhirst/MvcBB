using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using MvcBB.App.Interfaces;
using MvcBB.App.Mods;
using MvcBB.ModKit;

namespace MvcBB.App.TagHelpers
{
    /// <summary>
    /// Renders every enabled mod's contribution to a named view slot:
    /// <c>&lt;mod-slot name="@CoreSlots.PostAuthor" context="@postSlot" /&gt;</c>.
    ///
    /// No contributions, no enabled contributors, or nothing rendered: the tag
    /// writes nothing at all, so core markup is unchanged for a site without
    /// slot-using mods. A contribution that throws is logged and skipped; it
    /// never breaks the page. See docs/ModKit-ViewSlots.md.
    /// </summary>
    [HtmlTargetElement("mod-slot", TagStructure = TagStructure.WithoutEndTag)]
    public class ModSlotTagHelper : TagHelper
    {
        private const string EnabledIdsItemKey = "MvcBB.ModSlots.EnabledModIds";
        private const int DefaultSlowMilliseconds = 50;

        private readonly ModSlotRegistry _registry;
        private readonly IViewComponentHelper _viewComponentHelper;
        private readonly IModsService _modsService;
        private readonly ILogger<ModSlotTagHelper> _logger;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly int _slowMilliseconds;

        public ModSlotTagHelper(
            ModSlotRegistry registry,
            IViewComponentHelper viewComponentHelper,
            IModsService modsService,
            ILogger<ModSlotTagHelper> logger,
            HtmlEncoder htmlEncoder,
            IConfiguration configuration)
        {
            _registry = registry;
            _viewComponentHelper = viewComponentHelper;
            _modsService = modsService;
            _logger = logger;
            _htmlEncoder = htmlEncoder;
            _slowMilliseconds = configuration.GetValue("Mods:SlowSlotMilliseconds", DefaultSlowMilliseconds);
        }

        /// <summary>One of <see cref="CoreSlots"/>.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The slot's context object (its type is fixed per slot).</summary>
        public SlotContext? Context { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;

            // Fast path: nothing contributes to this slot, so no enabled-set
            // lookup and no allocation.
            var entries = _registry.GetEntries(Name);
            if (entries.Count == 0 || Context == null)
            {
                output.SuppressOutput();
                return;
            }

            var enabledIds = await GetEnabledModIdsAsync();
            if (enabledIds.Count == 0)
            {
                output.SuppressOutput();
                return;
            }

            var wrapper = ModSlotRegistry.KnownSlots.TryGetValue(Name, out var definition)
                ? definition.Wrapper
                : ModSlotRegistry.SlotWrapper.Block;

            (_viewComponentHelper as IViewContextAware)?.Contextualize(ViewContext);

            var rendered = new List<(string ModId, string Html)>();
            foreach (var entry in entries)
            {
                if (!enabledIds.Contains(entry.ModId))
                    continue;

                var html = await InvokeAsync(entry);
                if (!string.IsNullOrWhiteSpace(html))
                    rendered.Add((entry.ModId, html));
            }

            if (rendered.Count == 0)
            {
                output.SuppressOutput();
                return;
            }

            output.Content.Clear();
            if (wrapper == ModSlotRegistry.SlotWrapper.None)
            {
                foreach (var item in rendered)
                    output.Content.AppendHtml(item.Html);
                return;
            }

            var slotClass = "mod-slot mod-slot-" + Name.Replace('.', '-');
            output.Content.AppendHtml($"<div class=\"{_htmlEncoder.Encode(slotClass)}\">");
            foreach (var item in rendered)
            {
                output.Content.AppendHtml($"<div class=\"mod-slot-item\" data-mod=\"{_htmlEncoder.Encode(item.ModId)}\">");
                output.Content.AppendHtml(item.Html);
                output.Content.AppendHtml("</div>");
            }
            output.Content.AppendHtml("</div>");
        }

        private async Task<string?> InvokeAsync(ModSlotRegistry.Entry entry)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var content = await _viewComponentHelper.InvokeAsync(entry.ViewComponentType, new { context = Context });
                using var writer = new StringWriter();
                content.WriteTo(writer, _htmlEncoder);
                return writer.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mod slots: {ViewComponent} from mod \"{ModId}\" failed in slot \"{Slot}\" after {ElapsedMs} ms. Rendered nothing for it.",
                    entry.ViewComponentType.Name, entry.ModId, Name, stopwatch.ElapsedMilliseconds);
                return null;
            }
            finally
            {
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > _slowMilliseconds)
                {
                    _logger.LogWarning("Mod slots: {ViewComponent} from mod \"{ModId}\" took {ElapsedMs} ms in slot \"{Slot}\".",
                        entry.ViewComponentType.Name, entry.ModId, stopwatch.ElapsedMilliseconds, Name);
                }
            }
        }

        /// <summary>
        /// The enabled mod set, read once per request (a thread page renders
        /// slots for every post). If the API can't be reached, slots render
        /// nothing rather than failing the page.
        /// </summary>
        private async Task<HashSet<string>> GetEnabledModIdsAsync()
        {
            var items = ViewContext.HttpContext.Items;
            if (items.TryGetValue(EnabledIdsItemKey, out var cached) && cached is HashSet<string> cachedIds)
                return cachedIds;

            HashSet<string> enabledIds;
            try
            {
                enabledIds = await _modsService.GetEnabledModIdsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mod slots: couldn't read the enabled mod set, so slots render nothing on this request.");
                enabledIds = new HashSet<string>();
            }

            items[EnabledIdsItemKey] = enabledIds;
            return enabledIds;
        }
    }
}
