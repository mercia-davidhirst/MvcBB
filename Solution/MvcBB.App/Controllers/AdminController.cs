using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.Shared.Models.BBCode;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.App.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class AdminController : Controller
    {
        private readonly IMvcBBCodeService _bbCodeService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISettingsService _settingsService;

        public AdminController(IMvcBBCodeService bbCodeService, IWebHostEnvironment webHostEnvironment, ISettingsService settingsService)
        {
            _bbCodeService = bbCodeService;
            _webHostEnvironment = webHostEnvironment;
            _settingsService = settingsService;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public async Task<IActionResult> BBCode()
        {
            var tags = await _bbCodeService.GetBBCodeTagsAsync();
            return View(tags);
        }

        public IActionResult AddBBCode()
        {
            return View(new BBCodeTagModel());
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
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to update BBCode tag. Please try again.");
                return View(model);
            }
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

        public IActionResult AddSmilie()
        {
            return View(new SmilieViewModel());
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
                    // Save image file
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "smilies");
                    Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

                    var uniqueFileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    model.ImagePath = $"/images/smilies/{uniqueFileName}";
                }

                await _bbCodeService.AddSmilieAsync(model);
                TempData["Success"] = "Smilie added successfully.";
                return RedirectToAction(nameof(Smilies));
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
                    // Delete old image if it exists
                    var oldSmilie = await _bbCodeService.GetSmilieAsync(id);
                    if (oldSmilie != null && !string.IsNullOrEmpty(oldSmilie.ImagePath))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, oldSmilie.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Save new image file
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "smilies");
                    Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

                    var uniqueFileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    model.ImagePath = $"/images/smilies/{uniqueFileName}";
                }

                await _bbCodeService.UpdateSmilieAsync(id, model);
                TempData["Success"] = "Smilie updated successfully.";
                return RedirectToAction(nameof(Smilies));
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
                if (smilie != null && !string.IsNullOrEmpty(smilie.ImagePath))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, smilie.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                await _bbCodeService.DeleteSmilieAsync(id);
                TempData["Success"] = "Smilie deleted successfully.";
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
                return View(settings);
            }
            catch
            {
                return View(new DisplaySettings());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(DisplaySettings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _settingsService.UpdateDisplaySettingsAsync(model);
                TempData["Success"] = "Display settings saved successfully.";
            }
            catch
            {
                TempData["Error"] = "Failed to save display settings.";
            }

            return RedirectToAction(nameof(Settings));
        }
    }
} 