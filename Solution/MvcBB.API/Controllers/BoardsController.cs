using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BoardsController : ControllerBase
    {
        private readonly IBoardRepository _boardRepository;
        private readonly IForumThreadRepository _threadRepository;
        private readonly IUserRepository _userRepository;

        public BoardsController(
            IBoardRepository boardRepository,
            IForumThreadRepository threadRepository,
            IUserRepository userRepository)
        {
            _boardRepository = boardRepository;
            _threadRepository = threadRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<BoardResponse>> GetBoards()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var boards = _boardRepository.GetAll().Where(b => b.IsVisibleTo(isAuthenticated)).ToList();
            return Ok(MapToBoardResponses(boards));
        }

        [HttpGet("{id}")]
        public ActionResult<BoardResponse> GetBoard(int id)
        {
            var board = _boardRepository.GetById(id);
            if (board == null || !board.IsVisibleTo(User.Identity?.IsAuthenticated == true))
            {
                return NotFound(new { message = "Board not found" });
            }

            return Ok(MapToBoardResponses(new[] { board }).Single());
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public ActionResult<BoardResponse> CreateBoard(CreateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = new Board
            {
                Name = request.Name,
                Description = request.Description,
                SortOrder = request.SortOrder,
                ParentId = request.ParentId,
                IsCategory = request.IsCategory,
                IsPrivate = request.IsPrivate,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            board = _boardRepository.Add(board);

            return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, MapToBoardResponses(new[] { board }).Single());
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public ActionResult<BoardResponse> UpdateBoard(int id, UpdateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var board = _boardRepository.GetById(id);
            if (board == null)
            {
                return NotFound(new { message = "Board not found" });
            }

            board.Name = request.Name;
            board.Description = request.Description;
            board.SortOrder = request.SortOrder;
            board.IsActive = request.IsActive;
            board.ParentId = request.ParentId;
            board.IsCategory = request.IsCategory;
            board.IsPrivate = request.IsPrivate;
            board.UpdatedAt = DateTime.UtcNow;
            _boardRepository.Update(board);

            return Ok(MapToBoardResponses(new[] { board }).Single());
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBoard(int id)
        {
            var board = _boardRepository.GetById(id);
            if (board == null)
            {
                return NotFound();
            }

            _boardRepository.Remove(board);

            return NoContent();
        }

        /// <summary>
        /// Maps a page's worth of boards to responses with one batched query
        /// for "most recently active thread" across every board, instead of
        /// fetching every thread in a board (plus a username lookup) once per
        /// board. Derived from the boards' threads rather than board.LastPostAt/
        /// LastPostByUserId - those are only ever set when a thread is first
        /// created and go stale the moment someone replies to an existing one.
        /// </summary>
        private List<BoardResponse> MapToBoardResponses(IReadOnlyList<Board> boards)
        {
            if (boards.Count == 0)
            {
                return new List<BoardResponse>();
            }

            var lastActiveThreads = _threadRepository.GetLastActiveThreadsByBoardIds(boards.Select(b => b.Id));

            return boards.Select(board =>
            {
                var response = new BoardResponse
                {
                    Id = board.Id,
                    Name = board.Name,
                    Description = board.Description,
                    SortOrder = board.SortOrder,
                    IsActive = board.IsActive,
                    ParentId = board.ParentId,
                    IsCategory = board.IsCategory,
                    IsPrivate = board.IsPrivate,
                    CreatedAt = board.CreatedAt,
                    UpdatedAt = board.UpdatedAt,
                    ThreadCount = board.ThreadCount,
                    PostCount = board.PostCount
                };

                if (lastActiveThreads.TryGetValue(board.Id, out var lastThread))
                {
                    response.LastThreadId = lastThread.Id;
                    response.LastThreadTitle = lastThread.Title;
                    response.LastPostByUserId = lastThread.LastPostByUserId ?? string.Empty;
                    response.LastPostAt = lastThread.LastPostAt ?? lastThread.CreatedAt;
                    response.LastPostByUsername = lastThread.LastPostByUsername ?? string.Empty;
                }

                return response;
            }).ToList();
        }
    }
}
