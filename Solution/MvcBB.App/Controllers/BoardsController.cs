using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Board;

namespace MvcBB.App.Controllers
{
    public class BoardsController : Controller
    {
        private readonly IBoardService _boardService;
        private readonly IThreadService _threadService;

        public BoardsController(IBoardService boardService, IThreadService threadService)
        {
            _boardService = boardService;
            _threadService = threadService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var boards = await _boardService.GetBoardsAsync();
                return View(boards);
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load boards. Please try again later.";
                return View(Array.Empty<BoardResponse>());
            }
        }

        public async Task<IActionResult> Details(int id, int page = 1)
        {
            try
            {
                var board = await _boardService.GetBoardAsync(id);
                var threadResult = await _threadService.GetThreadsAsync(id, page);

                ViewData["Threads"] = threadResult.Threads;
                ViewData["Page"] = threadResult.Page;
                ViewData["TotalPages"] = threadResult.TotalPages;
                return View(board);
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load board details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = "ManageBoards")]
        public async Task<IActionResult> Create()
        {
            var boards = await _boardService.GetBoardsAsync();
            ViewData["Boards"] = boards;
            return View(new CreateBoardRequest { SortOrder = boards.Count() + 1 });
        }

        [HttpPost]
        [Authorize(Policy = "ManageBoards")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Boards"] = await _boardService.GetBoardsAsync();
                return View(request);
            }

            try
            {
                var board = await _boardService.CreateBoardAsync(request);

                if (board.IsCategory)
                {
                    TempData["Success"] = "Category created successfully.";
                    return RedirectToAction("Boards", "Admin");
                }

                TempData["Success"] = "Board created successfully.";
                return RedirectToAction(nameof(Details), new { id = board.Id });
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to create board. Please try again.");
                return View(request);
            }
        }

        [Authorize(Policy = "ManageBoards")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var board = await _boardService.GetBoardAsync(id);
                var updateRequest = new UpdateBoardRequest
                {
                    Name = board.Name,
                    Description = board.Description,
                    SortOrder = board.SortOrder,
                    IsActive = board.IsActive,
                    ParentId = board.ParentId,
                    IsCategory = board.IsCategory,
                    IsPrivate = board.IsPrivate
                };
                ViewData["Boards"] = (await _boardService.GetBoardsAsync()).Where(b => b.Id != id);
                return View(updateRequest);
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to load board for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Policy = "ManageBoards")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Boards"] = (await _boardService.GetBoardsAsync()).Where(b => b.Id != id);
                return View(request);
            }

            try
            {
                await _boardService.UpdateBoardAsync(id, request);
                TempData["Success"] = "Board updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (SessionExpiredException)
            {
                return await this.RedirectToSessionExpired();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to update board. Please try again.");
                return View(request);
            }
        }

        [HttpPost]
        [Authorize(Policy = "ManageBoards")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _boardService.DeleteBoardAsync(id);
                TempData["Success"] = "Board deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete board.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
} 