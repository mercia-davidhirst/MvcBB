using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Interfaces;

namespace MvcBB.App.ViewComponents
{
    public class AnnouncementBannerItem
    {
        public string Title { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;
    }

    /// <summary>
    /// Renders active announcements from _Layout.cshtml, so they show on every
    /// page (portal, boards, threads, members, search, profile, messages, ...)
    /// rather than requiring each controller to fetch and pass them through.
    /// </summary>
    public class AnnouncementBannerViewComponent : ViewComponent
    {
        private readonly IAnnouncementService _announcementService;
        private readonly IMvcBBCodeService _mvcBBCodeService;
        private readonly IBBCodeService _bbCodeService;

        public AnnouncementBannerViewComponent(
            IAnnouncementService announcementService,
            IMvcBBCodeService mvcBBCodeService,
            IBBCodeService bbCodeService)
        {
            _announcementService = announcementService;
            _mvcBBCodeService = mvcBBCodeService;
            _bbCodeService = bbCodeService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var announcements = await _announcementService.GetActiveAnnouncementsAsync();
                var smilies = await _mvcBBCodeService.GetAvailableSmiliesAsync();

                var items = announcements
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new AnnouncementBannerItem
                    {
                        Title = a.Title,
                        ContentHtml = _bbCodeService.ParseBBCode(a.Content, smilies)
                    })
                    .ToList();

                return View(items);
            }
            catch
            {
                // Don't let a transient API/session hiccup break every page's layout -
                // same reasoning as the unread-message-count fetch in _Layout.cshtml.
                return View(new List<AnnouncementBannerItem>());
            }
        }
    }
}
