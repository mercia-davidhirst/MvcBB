using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.WordFilter;

namespace MvcBB.API.Controllers
{
    /// <summary>
    /// Admin-only, unlike BBCodeController's tag/smilie endpoints - the
    /// censor list itself is a moderation tool, not something a visitor
    /// should be able to read (that would hand anyone an exact list of
    /// words to work around).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class WordFiltersController : ControllerBase
    {
        private readonly IWordFilterRepository _wordFilterRepository;

        public WordFiltersController(IWordFilterRepository wordFilterRepository)
        {
            _wordFilterRepository = wordFilterRepository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<WordFilterModel>> GetAll()
        {
            return Ok(_wordFilterRepository.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<WordFilterModel> GetById(int id)
        {
            var filter = _wordFilterRepository.GetById(id);
            if (filter == null)
            {
                return NotFound(new { message = "Word filter not found" });
            }

            return Ok(filter);
        }

        [HttpPost]
        public ActionResult<WordFilterModel> Create(WordFilterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_wordFilterRepository.GetAll().Any(f => string.Equals(f.Word, model.Word, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Word), "A filter for this word already exists.");
                return BadRequest(ModelState);
            }

            var created = _wordFilterRepository.Add(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public ActionResult<WordFilterModel> Update(int id, WordFilterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_wordFilterRepository.GetById(id) == null)
            {
                return NotFound(new { message = "Word filter not found" });
            }

            if (_wordFilterRepository.GetAll().Any(f => f.Id != id && string.Equals(f.Word, model.Word, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Word), "A filter for this word already exists.");
                return BadRequest(ModelState);
            }

            model.Id = id;
            _wordFilterRepository.Update(model);
            return Ok(model);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var existing = _wordFilterRepository.GetById(id);
            if (existing == null)
            {
                return NotFound(new { message = "Word filter not found" });
            }

            _wordFilterRepository.Remove(existing);
            return NoContent();
        }
    }
}
