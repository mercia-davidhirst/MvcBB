using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Models;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.Search;

namespace MvcBB.App.Controllers
{
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;
        private readonly IBoardService _boardService;

        public SearchController(ISearchService searchService, IBoardService boardService)
        {
            _searchService = searchService;
            _boardService = boardService;
        }

        public async Task<IActionResult> Index(string q, string type = null, int? boardId = null, int page = 1)
        {
            const int PageSize = 20;

            if (string.IsNullOrWhiteSpace(q))
            {
                return View(new SearchViewModel
                {
                    Query = q,
                    Type = type,
                    BoardId = boardId,
                    Page = page,
                    PageSize = PageSize
                });
            }

            try
            {
                var searchResponse = await _searchService.SearchAsync(q, type, boardId, page, PageSize);
                var boards = await _boardService.GetBoardsAsync();

                var viewModel = new SearchViewModel
                {
                    Query = q,
                    Type = type,
                    BoardId = boardId,
                    Page = searchResponse.Page,
                    PageSize = searchResponse.PageSize,
                    TotalResults = searchResponse.TotalResults,
                    TotalPages = searchResponse.TotalPages,
                    Results = searchResponse.Results,
                    Boards = boards
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                ModelState.AddModelError("", "An error occurred while searching. Please try again.");
                return View(new SearchViewModel
                {
                    Query = q,
                    Type = type,
                    BoardId = boardId,
                    Page = page,
                    PageSize = PageSize
                });
            }
        }
    }
} 