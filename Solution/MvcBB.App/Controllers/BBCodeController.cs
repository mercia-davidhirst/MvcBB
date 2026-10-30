using Microsoft.AspNetCore.Mvc;
using MvcBB.Shared.Interfaces;

namespace MvcBB.App.Controllers
{
    [Route("api/bbcode")]
    public class BBCodeController : Controller
    {
        private readonly IBBCodeService _bbCodeService;

        public BBCodeController(IBBCodeService bbCodeService)
        {
            _bbCodeService = bbCodeService;
        }

        public class PreviewRequest
        {
            public string Content { get; set; } = string.Empty;
        }

        [HttpPost("preview")]
        public IActionResult Preview([FromBody] PreviewRequest request)
        {
            var html = _bbCodeService.ParseBBCode(request.Content);
            return Content(html, "text/html");
        }
    }
}
