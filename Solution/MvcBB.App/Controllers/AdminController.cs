using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Models;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.App.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class AdminController : Controller
    {
        private readonly IBBCodeManagementService _bbCodeService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(IBBCodeManagementService bbCodeService, IWebHostEnvironment webHostEnvironment)
        {
            _bbCodeService = bbCodeService;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult BBCode()
        {
            var tags = _bbCodeService.GetBBCodeTags();
            return View(tags);
        }

        public IActionResult AddBBCode()
        {
            return View(new BBCodeTagModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBBCode(BBCodeTagModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _bbCodeService.AddBBCodeTag(model);
                TempData["Success"] = "BBCode tag added successfully.";
                return RedirectToAction(nameof(BBCode));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to add BBCode tag. Please try again.");
                return View(model);
            }
        }

        public IActionResult EditBBCode(int id)
        {
            var tag = _bbCodeService.GetBBCodeTag(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditBBCode(int id, BBCodeTagModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _bbCodeService.UpdateBBCodeTag(id, model);
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
        public IActionResult DeleteBBCode(int id)
        {
            try
            {
                _bbCodeService.DeleteBBCodeTag(id);
                TempData["Success"] = "BBCode tag deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to delete BBCode tag.";
            }
            return RedirectToAction(nameof(BBCode));
        }

        public IActionResult Smilies()
        {
            var smilies = _bbCodeService.GetSmilies();
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

                _bbCodeService.AddSmilie(model);
                TempData["Success"] = "Smilie added successfully.";
                return RedirectToAction(nameof(Smilies));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to add smilie. Please try again.");
                return View(model);
            }
        }

        public IActionResult EditSmilie(int id)
        {
            var smilie = _bbCodeService.GetSmilie(id);
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
                    var oldSmilie = _bbCodeService.GetSmilie(id);
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

                _bbCodeService.UpdateSmilie(id, model);
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
        public IActionResult DeleteSmilie(int id)
        {
            try
            {
                var smilie = _bbCodeService.GetSmilie(id);
                if (smilie != null && !string.IsNullOrEmpty(smilie.ImagePath))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, smilie.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _bbCodeService.DeleteSmilie(id);
                TempData["Success"] = "Smilie deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to delete smilie.";
            }
            return RedirectToAction(nameof(Smilies));
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
} 