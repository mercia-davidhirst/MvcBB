using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Rank;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RanksController : ControllerBase
    {
        private readonly IRankRepository _rankRepository;

        public RanksController(IRankRepository rankRepository)
        {
            _rankRepository = rankRepository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<RankModel>> GetAll()
        {
            return Ok(_rankRepository.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<RankModel> GetById(int id)
        {
            var rank = _rankRepository.GetById(id);
            if (rank == null)
            {
                return NotFound(new { message = "Rank not found" });
            }

            return Ok(rank);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public ActionResult<RankModel> Create(RankModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = _rankRepository.Add(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public ActionResult<RankModel> Update(int id, RankModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_rankRepository.GetById(id) == null)
            {
                return NotFound(new { message = "Rank not found" });
            }

            model.Id = id;
            _rankRepository.Update(model);
            return Ok(model);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public IActionResult Delete(int id)
        {
            var existing = _rankRepository.GetById(id);
            if (existing == null)
            {
                return NotFound(new { message = "Rank not found" });
            }

            _rankRepository.Remove(existing);
            return NoContent();
        }
    }
}
