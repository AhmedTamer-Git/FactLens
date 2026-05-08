using Factlens.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Factlens.Controllers
{
    [ApiController]
    [Route("api/public")]
    public class PublicController : ControllerBase
    {
        private readonly IShareService _shareService;

        public PublicController(IShareService shareService)
        {
            _shareService = shareService;
        }

        // ✅ Public Share
        // GET: /api/public/share/{shareId}
        [HttpGet("share/{shareId}")]
        public async Task<IActionResult> GetShared(string shareId)
        {
            try
            {
                var result = await _shareService.GetSharedAsync(shareId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("share/{shareId}/image")]
        public async Task<IActionResult> ExportImage(string shareId)
        {
            try
            {
                var file = await _shareService.ExportImageAsync(shareId);
                return File(file, "image/png", "FactLens_Result.png");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    innerInner = ex.InnerException?.InnerException?.Message,
                    type = ex.GetType().FullName,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}