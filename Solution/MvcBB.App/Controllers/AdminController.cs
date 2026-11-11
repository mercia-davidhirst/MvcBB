using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.Shared.Models.BBCode;
using MvcBB.Shared.Models.Rank;
using MvcBB.Shared.Models.WordFilter;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.App.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class AdminController : Controller
    {
        private readonly IMvcBBCodeService _bbCodeService;
        private readonly ISettingsService _settingsService;
        private readonly IBoardService _boardService;
        private readonly IImageUploadService _imageUploadService;
        private readonly IRankService _rankService;
        private readonly IWordFilterService _wordFilterService;

        public AdminController(
            IMvcBBCodeService bbCodeService,
            ISettingsService settingsService,
            IBoardService boardService,
            IImageUploadService imageUploadService,
            IRankService rankService,
            IWordFilterService wordFilterService)
        {
            _bbCodeService = bbCodeService;
            _settingsService = settingsService;
            _boardService = boardService;
            _imageUploadService = imageUploadService;
            _rankService = rankService;
            _wordFilterService = wordFilterService;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public async Task<IActionResult> Boards()
        {
            var boards = await _boardService.GetBoardsAsync();
            return View(boards.OrderBy(b => b.ParentId).ThenBy(b => b.SortOrder));
        }

        public async Task<IActionResult> BBCode()
        {
            var tags = await _bbCodeService.GetBBCodeTagsAsync();
            return View(tags);
        }

        public async Task<IActionResult> AddBBCode()
        {
            var tags = await _bbCodeService.GetBBCodeTagsAsync();
            return View(new BBCodeTagModel { SortOrder = tags.Count() + 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBBCode(BBCodeTagModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _bbCodeService.AddBBCodeTagAsync(model);
                TempData["Success"] = "BBCode tag added successfully.";
                return RedirectToAction(nameof(BBCode));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to add BBCode tag. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> EditBBCode(int id)
        {
            var tag = await _bbCodeService.GetBBCodeTagAsync(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBBCode(int id, BBCodeTagModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _bbCodeService.UpdateBBCodeTagAsync(id, model);
                TempData["Success"] = "BBCode tag updated successfully.";
                return RedirectToAction(nameof(BBCode));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to update BBCode tag. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Ranks()
        {
            var ranks = await _rankService.GetRanksAsync();
            return View(ranks.OrderBy(r => r.SortOrder));
        }

        public async Task<IActionResult> AddRank()
        {
            var ranks = await _rankService.GetRanksAsync();
            return View(new RankViewModel { SortOrder = ranks.Count() + 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRank(RankViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.ImageFile != null)
                {
                    model.ImagePath = await _imageUploadService.SaveImageAsync(model.ImageFile, "ranks");
                }

                await _rankService.AddRankAsync(model);
                TempData["Success"] = "Rank added successfully.";
                return RedirectToAction(nameof(Ranks));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to add rank. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> EditRank(int id)
        {
            var rank = await _rankService.GetRankAsync(id);
            if (rank == null)
            {
                return NotFound();
            }
            var viewModel = new RankViewModel
            {
                Id = rank.Id,
                Title = rank.Title,
                MinPosts = rank.MinPosts,
                ImagePath = rank.ImagePath,
                IsActive = rank.IsActive,
                SortOrder = rank.SortOrder
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRank(int id, RankViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.ImageFile != null)
                {
                    var oldRank = await _rankService.GetRankAsync(id);
                    model.ImagePath = await _imageUploadService.SaveImageAsync(model.ImageFile, "ranks");
                    _imageUploadService.DeleteImage(oldRank?.ImagePath);
                }

                await _rankService.UpdateRankAsync(id, model);
                TempData["Success"] = "Rank updated successfully.";
                return RedirectToAction(nameof(Ranks));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to update rank. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRank(int id)
        {
            try
            {
                await _rankService.DeleteRankAsync(id);
                TempData["Success"] = "Rank deleted successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete rank.";
            }
            return RedirectToAction(nameof(Ranks));
        }

        public async Task<IActionResult> WordFilters()
        {
            var filters = await _wordFilterService.GetWordFiltersAsync();
            return View(filters.OrderBy(f => f.SortOrder));
        }

        public async Task<IActionResult> AddWordFilter()
        {
            var filters = await _wordFilterService.GetWordFiltersAsync();
            return View(new WordFilterModel { SortOrder = filters.Count() + 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWordFilter(WordFilterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _wordFilterService.AddWordFilterAsync(model);
                TempData["Success"] = "Word filter added successfully.";
                return RedirectToAction(nameof(WordFilters));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to add word filter. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> EditWordFilter(int id)
        {
            var filter = await _wordFilterService.GetWordFilterAsync(id);
            if (filter == null)
            {
                return NotFound();
            }
            return View(filter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWordFilter(int id, WordFilterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _wordFilterService.UpdateWordFilterAsync(id, model);
                TempData["Success"] = "Word filter updated successfully.";
                return RedirectToAction(nameof(WordFilters));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to update word filter. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWordFilter(int id)
        {
            try
            {
                await _wordFilterService.DeleteWordFilterAsync(id);
                TempData["Success"] = "Word filter deleted successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete word filter.";
            }
            return RedirectToAction(nameof(WordFilters));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBBCode(int id)
        {
            try
            {
                await _bbCodeService.DeleteBBCodeTagAsync(id);
                TempData["Success"] = "BBCode tag deleted successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to delete BBCode tag.";
            }
            return RedirectToAction(nameof(BBCode));
        }

        public async Task<IActionResult> Smilies()
        {
            var smilies = await _bbCodeService.GetSmiliesAsync();
            return View(smilies);
        }

        public async Task<IActionResult> AddSmilie()
        {
            var smilies = await _bbCodeService.GetSmiliesAsync();
            return View(new SmilieViewModel { SortOrder = smilies.Count() + 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSmilie(SmilieViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.ImageFile != null)
                {
                    model.ImagePath = await _imageUploadService.SaveImageAsync(model.ImageFile, "smilies");
                }

                await _bbCodeService.AddSmilieAsync(model);
                TempData["Success"] = "Smilie added successfully.";
                return RedirectToAction(nameof(Smilies));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to add smilie. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> EditSmilie(int id)
        {
            var smilie = await _bbCodeService.GetSmilieAsync(id);
            if (smilie == null)
            {
                return NotFound();
            }
            var viewModel = new SmilieViewModel
            {
                Id = smilie.Id,
                Code = smilie.Code,
                Description = smilie.Description,
                ImagePath = smilie.ImagePath,
                IsActive = smilie.IsActive,
                SortOrder = smilie.SortOrder
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSmilie(int id, SmilieViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.ImageFile != null)
                {
                    var oldSmilie = await _bbCodeService.GetSmilieAsync(id);
                    model.ImagePath = await _imageUploadService.SaveImageAsync(model.ImageFile, "smilies");
                    _imageUploadService.DeleteImage(oldSmilie?.ImagePath);
                }

                await _bbCodeService.UpdateSmilieAsync(id, model);
                TempData["Success"] = "Smilie updated successfully.";
                return RedirectToAction(nameof(Smilies));
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to update smilie. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSmilie(int id)
        {
            try
            {
                var smilie = await _bbCodeService.GetSmilieAsync(id);
                _imageUploadService.DeleteImage(smilie?.ImagePath);

                await _bbCodeService.DeleteSmilieAsync(id);
                TempData["Success"] = "Smilie deleted successfully.";
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to delete smilie.";
            }
            return RedirectToAction(nameof(Smilies));
        }

        public async Task<IActionResult> Settings()
        {
            try
            {
                var settings = await _settingsService.GetDisplaySettingsAsync();
                return View(ToViewModel(settings));
            }
            catch
            {
                return View(new DisplaySettingsViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(DisplaySettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.LogoFile != null)
                {
                    var oldSettings = await _settingsService.GetDisplaySettingsAsync();
                    model.LogoPath = await _imageUploadService.SaveImageAsync(model.LogoFile, "logos");
                    _imageUploadService.DeleteImage(oldSettings.LogoPath);
                }
                else if (model.RemoveLogo)
                {
                    var oldSettings = await _settingsService.GetDisplaySettingsAsync();
                    _imageUploadService.DeleteImage(oldSettings.LogoPath);
                    model.LogoPath = null;
                }

                await _settingsService.UpdateDisplaySettingsAsync(model);
                TempData["Success"] = "Display settings saved successfully.";
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch
            {
                TempData["Error"] = "Failed to save display settings.";
            }

            return RedirectToAction(nameof(Settings));
        }

        private static DisplaySettingsViewModel ToViewModel(DisplaySettings settings) => new()
        {
            ThreadsPerPage = settings.ThreadsPerPage,
            SiteTitle = settings.SiteTitle,
            WelcomeMessage = settings.WelcomeMessage,
            SiteName = settings.SiteName,
            LogoPath = settings.LogoPath,
            FooterDisclaimer = settings.FooterDisclaimer
        };
    }
} 