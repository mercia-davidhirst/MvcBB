using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BBCodeController : ControllerBase
    {
        private readonly IBBCodeManagementService _bbCodeService;

        public BBCodeController(IBBCodeManagementService bbCodeService)
        {
            _bbCodeService = bbCodeService;
        }

        [HttpGet("tags")]
        public ActionResult<IEnumerable<BBCodeTagModel>> GetTags()
        {
            return Ok(_bbCodeService.GetBBCodeTags());
        }

        [HttpGet("tags/{id}")]
        public ActionResult<BBCodeTagModel> GetTag(int id)
        {
            var tag = _bbCodeService.GetBBCodeTag(id);
            if (tag == null)
            {
                return NotFound(new { message = "BBCode tag not found" });
            }

            return Ok(tag);
        }

        [HttpPost("tags")]
        [Authorize(Roles = "Administrator")]
        public ActionResult<BBCodeTagModel> CreateTag(BBCodeTagModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_bbCodeService.GetBBCodeTags().Any(t => string.Equals(t.Name, model.Name, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Name), "A BBCode tag with this name already exists.");
                return BadRequest(ModelState);
            }

            _bbCodeService.AddBBCodeTag(model);
            return CreatedAtAction(nameof(GetTag), new { id = model.Id }, model);
        }

        [HttpPut("tags/{id}")]
        [Authorize(Roles = "Administrator")]
        public ActionResult<BBCodeTagModel> UpdateTag(int id, BBCodeTagModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_bbCodeService.GetBBCodeTag(id) == null)
            {
                return NotFound(new { message = "BBCode tag not found" });
            }

            if (_bbCodeService.GetBBCodeTags().Any(t => t.Id != id && string.Equals(t.Name, model.Name, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Name), "A BBCode tag with this name already exists.");
                return BadRequest(ModelState);
            }

            _bbCodeService.UpdateBBCodeTag(id, model);
            return Ok(model);
        }

        [HttpDelete("tags/{id}")]
        [Authorize(Roles = "Administrator")]
        public IActionResult DeleteTag(int id)
        {
            if (_bbCodeService.GetBBCodeTag(id) == null)
            {
                return NotFound(new { message = "BBCode tag not found" });
            }

            _bbCodeService.DeleteBBCodeTag(id);
            return NoContent();
        }

        [HttpGet("smilies")]
        public ActionResult<IEnumerable<SmilieModel>> GetSmilies()
        {
            return Ok(_bbCodeService.GetSmilies());
        }

        [HttpGet("smilies/{id}")]
        public ActionResult<SmilieModel> GetSmilie(int id)
        {
            var smilie = _bbCodeService.GetSmilie(id);
            if (smilie == null)
            {
                return NotFound(new { message = "Smilie not found" });
            }

            return Ok(smilie);
        }

        [HttpPost("smilies")]
        [Authorize(Roles = "Administrator")]
        public ActionResult<SmilieModel> CreateSmilie(SmilieModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_bbCodeService.GetSmilies().Any(s => string.Equals(s.Code, model.Code, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Code), "A smilie with this code already exists.");
                return BadRequest(ModelState);
            }

            _bbCodeService.AddSmilie(model);
            return CreatedAtAction(nameof(GetSmilie), new { id = model.Id }, model);
        }

        [HttpPut("smilies/{id}")]
        [Authorize(Roles = "Administrator")]
        public ActionResult<SmilieModel> UpdateSmilie(int id, SmilieModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_bbCodeService.GetSmilie(id) == null)
            {
                return NotFound(new { message = "Smilie not found" });
            }

            if (_bbCodeService.GetSmilies().Any(s => s.Id != id && string.Equals(s.Code, model.Code, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Code), "A smilie with this code already exists.");
                return BadRequest(ModelState);
            }

            _bbCodeService.UpdateSmilie(id, model);
            return Ok(model);
        }

        [HttpDelete("smilies/{id}")]
        [Authorize(Roles = "Administrator")]
        public IActionResult DeleteSmilie(int id)
        {
            if (_bbCodeService.GetSmilie(id) == null)
            {
                return NotFound(new { message = "Smilie not found" });
            }

            _bbCodeService.DeleteSmilie(id);
            return NoContent();
        }
    }
} 