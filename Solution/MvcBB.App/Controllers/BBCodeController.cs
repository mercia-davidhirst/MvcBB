using Microsoft.AspNetCore.Mvc;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Interfaces;

namespace MvcBB.App.Controllers
{
    [Route("api/bbcode")]
    public class BBCodeController : Controller
    {
        private readonly IBBCodeService _bbCodeService;
        private readonly IMvcBBCodeService _mvcBBCodeService;

        public BBCodeController(IBBCodeService bbCodeService, IMvcBBCodeService mvcBBCodeService)
        {
            _bbCodeService = bbCodeService;
            _mvcBBCodeService = mvcBBCodeService;
        }

        public class PreviewRequest
        {
            public string Content { get; set; } = string.Empty;
        }

        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] PreviewRequest request)
        {
            var smilies = await _mvcBBCodeService.GetAvailableSmiliesAsync();
            var html = _bbCodeService.ParseBBCode(request.Content, smilies);
            return Content(html, "text/html");
        }
    }
}
