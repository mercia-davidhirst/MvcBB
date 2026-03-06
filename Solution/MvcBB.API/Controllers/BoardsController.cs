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

        public BoardsController(IBoardRepository boardRepository)
        {
            _boardRepository = boardRepository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Board>> GetBoards()
        {
            return Ok(_boardRepository.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<Board> GetBoard(int id)
        {
            var board = _boardRepository.GetById(id);
            if (board == null)
            {
                return NotFound(new { message = "Board not found" });
            }

            return Ok(board);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public ActionResult<Board> CreateBoard(CreateBoardRequest request)
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
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            board = _boardRepository.Add(board);

            return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, board);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public ActionResult<Board> UpdateBoard(int id, UpdateBoardRequest request)
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
            board.UpdatedAt = DateTime.UtcNow;
            _boardRepository.Update(board);

            return Ok(board);
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
    }
}
