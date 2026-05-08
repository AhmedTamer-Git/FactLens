using Factlens.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Factlens.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminDashboardService _admin;

        public AdminController(IAdminDashboardService admin)
        {
            _admin = admin;
        }

        // ✅ 1) Summary
        // GET: /api/admin/summary
        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var result = await _admin.SummaryAsync();
            return Ok(result);
        }

        // ✅ 2) Requests logs
        // GET: /api/admin/requests?page=1&pageSize=50&status=200&search=text
        [HttpGet("requests")]
        public async Task<IActionResult> Requests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? status = null,
            [FromQuery] string? search = null)
        {
            var result = await _admin.RequestsAsync(page, pageSize, status, search);
            return Ok(result);
        }

        // ✅ 3) Verdicts breakdown
        // GET: /api/admin/verdicts
        [HttpGet("verdicts")]
        public async Task<IActionResult> Verdicts()
        {
            var result = await _admin.VerdictsAsync();
            return Ok(result);
        }

        // ✅ 4) Feedback list
        // GET: /api/admin/feedback?page=1&pageSize=50&reportedOnly=true
        [HttpGet("feedback")]
        public async Task<IActionResult> Feedback(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool? reportedOnly = null)
        {
            var result = await _admin.FeedbackAsync(page, pageSize, reportedOnly);
            return Ok(result);
        }
    }
}
