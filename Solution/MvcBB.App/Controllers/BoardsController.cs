using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load boards. Please try again later.";
                return View(Array.Empty<Board>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var board = await _boardService.GetBoardAsync(id);
                var threadResult = await _threadService.GetThreadsAsync(id);
                
                ViewData["Threads"] = threadResult.Threads;
                return View(board);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load board details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = "ManageBoards")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "ManageBoards")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var board = await _boardService.CreateBoardAsync(request);
                TempData["Success"] = "Board created successfully.";
                return RedirectToAction(nameof(Details), new { id = board.Id });
            }
            catch (Exception ex)
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
                    Description = board.Description
                };
                return View(updateRequest);
            }
            catch (Exception ex)
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
                return View(request);
            }

            try
            {
                await _boardService.UpdateBoardAsync(id, request);
                TempData["Success"] = "Board updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to delete board.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
} 