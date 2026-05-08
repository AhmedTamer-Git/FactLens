using Factlens.Core.DTOs;
using Factlens.Core.Models;
using Factlens.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Factlens.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IAiOrchestrator _aiOrchestrator;
        private readonly IHistoryService _historyService;
        private readonly IFeedbackService _feedbackService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(
            IAiOrchestrator aiOrchestrator,
            IHistoryService historyService,
            IFeedbackService feedbackService,
            UserManager<ApplicationUser> userManager)
        {
            _aiOrchestrator = aiOrchestrator;
            _historyService = historyService;
            _feedbackService = feedbackService;
            _userManager = userManager;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        // ===================================================
        // 1) Check News (Text / URL)
        // POST: /api/user/check-news
        // ===================================================
        [HttpPost("check-news")]
        public async Task<IActionResult> CheckNews([FromBody] NewRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text is required.");

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Invalid token user id.");

            try
            {
                var aiResponse = await _aiOrchestrator.CheckNewsAndSaveAsync(userId, request.Text);
                return Ok(aiResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ===================================================
        // 2) Check News (Image)
        // POST: /api/user/check-image
        // Content-Type: multipart/form-data
        // ===================================================
        [HttpPost("check-image")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
        public async Task<IActionResult> CheckImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Image file is required.");

            // التحقق من نوع الملف
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Only JPEG, PNG, and WebP images are supported.");

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Invalid token user id.");

            try
            {
                using var stream = file.OpenReadStream();
                var aiResponse = await _aiOrchestrator.CheckNewsImageAndSaveAsync(
                    userId, stream, file.FileName);
                return Ok(aiResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ===================================================
        // 3) History
        // GET: /api/user/history
        // ===================================================
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string? search,
            [FromQuery] string? verdict,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Invalid token user id.");

            var result = await _historyService.GetAsync(userId, search, verdict, from, to, page, pageSize);
            return Ok(result);
        }

        // ===================================================
        // 4) Delete one history record
        // DELETE: /api/user/history/{id}
        // ===================================================
        [HttpDelete("history/{id:int}")]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Invalid token user id.");

            try
            {
                await _historyService.DeleteAsync(userId, id);
                return Ok(new { message = "تم حذف العنصر بنجاح." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // ===================================================
        // 5) Clear history
        // DELETE: /api/user/history/clear
        // ===================================================
        [HttpDelete("history/clear")]
        public async Task<IActionResult> ClearHistory()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Invalid token user id.");

            await _historyService.ClearAsync(userId);
            return Ok(new { message = "تم مسح الهيستوري بالكامل." });
        }

        // ===================================================
        // 6) Feedback
        // POST: /api/user/feedback
        // ===================================================
        [HttpPost("feedback")]
        public async Task<IActionResult> AddFeedback([FromBody] FeedbackRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Invalid token user id.");

            try
            {
                await _feedbackService.AddAsync(userId, request);
                return Ok(new { message = "تم إرسال الفيدباك بنجاح." });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        // ===================================================
        // 7) GET Profile
        // GET: /api/user/profile
        // ===================================================
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var hasPassword = await _userManager.HasPasswordAsync(user);

            return Ok(new
            {
                fullName = user.FullName,
                username = user.UserName,
                email = user.Email,
                phone = user.Phone,
                age = user.Age,
                hasPassword
            });
        }

        // ===================================================
        // 8) UPDATE Profile
        // PUT: /api/user/profile
        // ===================================================
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName.Trim();

            if (dto.Phone != null)
                user.Phone = dto.Phone.Trim();

            if (dto.Age.HasValue && dto.Age >= 13 && dto.Age <= 120)
                user.Age = dto.Age.Value;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                message = "Profile updated successfully",
                fullName = user.FullName,
                username = user.UserName,
                email = user.Email,
                phone = user.Phone,
                age = user.Age
            });
        }

        // ===================================================
        // 9) CHANGE / SET Password
        // POST: /api/user/change-password
        // ===================================================
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("New password and confirmation do not match");

            var hasPassword = await _userManager.HasPasswordAsync(user);
            IdentityResult result;

            if (hasPassword)
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                    return BadRequest("Current password is required");

                result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            }
            else
            {
                result = await _userManager.AddPasswordAsync(user, dto.NewPassword);
            }

            if (!result.Succeeded)
                return BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));

            return Ok(new
            {
                message = hasPassword ? "Password changed successfully" : "Password set successfully"
            });
        }

        // ===================================================
        // 10) DELETE Account
        // DELETE: /api/user/account
        // ===================================================
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword && !await _userManager.CheckPasswordAsync(user, dto.Password))
                return BadRequest("Incorrect password");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Account deleted successfully");
        }
    }
}