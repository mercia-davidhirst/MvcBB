using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Models.Board;
using System.Security.Claims;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BoardsController : ControllerBase
    {
        private static readonly List<Board> _boards = new();

        [HttpGet]
        public ActionResult<IEnumerable<Board>> GetBoards()
        {
            return Ok(_boards.OrderBy(b => b.SortOrder));
        }

        [HttpGet("{id}")]
        public ActionResult<Board> GetBoard(int id)
        {
            var board = _boards.FirstOrDefault(b => b.Id == id);
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
                Id = _boards.Count + 1,
                Name = request.Name,
                Description = request.Description,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _boards.Add(board);

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

            var board = _boards.FirstOrDefault(b => b.Id == id);
            if (board == null)
            {
                return NotFound(new { message = "Board not found" });
            }

            board.Name = request.Name;
            board.Description = request.Description;
            board.SortOrder = request.SortOrder;
            board.IsActive = request.IsActive;
            board.UpdatedAt = DateTime.UtcNow;

            return Ok(board);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBoard(int id)
        {
            var board = _boards.FirstOrDefault(b => b.Id == id);
            if (board == null)
            {
                return NotFound();
            }

            _boards.Remove(board);

            return NoContent();
        }
    }
} 