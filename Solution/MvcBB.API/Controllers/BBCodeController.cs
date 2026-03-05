using Microsoft.AspNetCore.Mvc;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BBCodeController : ControllerBase
    {
        private readonly ICoreBBCodeService _bbCodeService;

        public BBCodeController(ICoreBBCodeService bbCodeService)
        {
            _bbCodeService = bbCodeService;
        }

        [HttpPost("parse")]
        public ActionResult<string> ParseBBCode([FromBody] string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest(new { message = "Content is required" });
            }

            var parsedContent = _bbCodeService.ParseBBCode(content);
            return Ok(new { content = parsedContent });
        }

        [HttpPost("strip")]
        public ActionResult<string> StripBBCode([FromBody] string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest(new { message = "Content is required" });
            }

            var strippedContent = _bbCodeService.StripBBCode(content);
            return Ok(new { content = strippedContent });
        }

        [HttpPost("validate")]
        public ActionResult<bool> ValidateBBCode([FromBody] string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest(new { message = "Content is required" });
            }

            var isValid = _bbCodeService.ValidateBBCode(content);
            return Ok(new { isValid });
        }
    }
} 