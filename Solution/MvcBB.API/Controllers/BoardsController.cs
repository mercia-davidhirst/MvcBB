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
            return Ok(_boardRepository.GetAll()
                .Where(b => b.IsVisibleTo(isAuthenticated))
                .Select(MapToBoardResponse));
        }

        [HttpGet("{id}")]
        public ActionResult<BoardResponse> GetBoard(int id)
        {
            var board = _boardRepository.GetById(id);
            if (board == null || !board.IsVisibleTo(User.Identity?.IsAuthenticated == true))
            {
                return NotFound(new { message = "Board not found" });
            }

            return Ok(MapToBoardResponse(board));
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

            return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, MapToBoardResponse(board));
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

            return Ok(MapToBoardResponse(board));
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

        private BoardResponse MapToBoardResponse(Board board)
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

            // Derived from the board's own threads rather than board.LastPostAt/
            // LastPostByUserId - those are only ever set when a thread is first
            // created and go stale the moment someone replies to an existing one.
            var lastThread = _threadRepository.GetByBoardId(board.Id)
                .OrderByDescending(t => t.LastPostAt ?? t.CreatedAt)
                .FirstOrDefault();

            if (lastThread != null)
            {
                response.LastThreadId = lastThread.Id;
                response.LastThreadTitle = lastThread.Title;
                response.LastPostByUserId = lastThread.LastPostByUserId ?? string.Empty;
                response.LastPostAt = lastThread.LastPostAt ?? lastThread.CreatedAt;

                if (!string.IsNullOrEmpty(lastThread.LastPostByUserId) &&
                    int.TryParse(lastThread.LastPostByUserId, out var lastPostUserId))
                {
                    response.LastPostByUsername = _userRepository.GetById(lastPostUserId)?.Username ?? string.Empty;
                }
            }

            return response;
        }
    }
}
