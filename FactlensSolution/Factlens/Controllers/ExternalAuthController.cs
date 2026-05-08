using Factlens.Core.Models;
using Factlens.Services.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Factlens.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtService _jwtService;

        public ExternalAuthController(UserManager<ApplicationUser> userManager, JwtService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
        }

        // ✅ يبدأ تسجيل الدخول بجوجل
        [HttpGet("google")]
        public IActionResult GoogleLogin([FromQuery] string? returnUrl)
        {
            // ✅ لو مفيش returnUrl هنرجع swagger
            returnUrl ??= $"{Request.Scheme}://{Request.Host}/swagger";

            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl })
            };

            return Challenge(props, "Google");
        }

        // ✅ جوجل بيرجع هنا
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string returnUrl)
        {
            var result = await HttpContext.AuthenticateAsync("External");

            if (!result.Succeeded || result.Principal == null)
                return BadRequest("External authentication failed.");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

            if (string.IsNullOrEmpty(email))
                return BadRequest("Google account has no email.");

            var user = await _userManager.FindByEmailAsync(email);

            // ✅ لو المستخدم جديد: ننشئه
            if (user == null)
            {
                // ✅ username مبني على الاسم + يتأكد إنه unique
                var baseUsername = (name ?? "user")
                    .Trim()
                    .ToLower()
                    .Replace(" ", "");

                if (string.IsNullOrWhiteSpace(baseUsername))
                    baseUsername = "user";

                var username = baseUsername;
                var i = 1;

                while (await _userManager.FindByNameAsync(username) != null)
                {
                    username = $"{baseUsername}{i}";
                    i++;
                }

                user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true
                };

                var createRes = await _userManager.CreateAsync(user);
                if (!createRes.Succeeded)
                    return BadRequest(createRes.Errors);

                // ✅ (اختياري) ضيفه Role User
                if (!await _userManager.IsInRoleAsync(user, "User"))
                    await _userManager.AddToRoleAsync(user, "User");
            }

            // ✅ هات Roles للمستخدم علشان تدخل في الـ JWT
            var roles = await _userManager.GetRolesAsync(user);

            // ✅ Generate JWT (بـ roles) — تعديل مهم
            var token = _jwtService.GenerateToken(user.Id, user.Email,user.UserName ,roles);

            // ✅ نخرج من External Cookie
            await HttpContext.SignOutAsync("External");

            // ✅ رجّع للفرونت بالتوكن في query string
            var separator = returnUrl.Contains("?") ? "&" : "?";
            return Redirect($"{returnUrl}{separator}token={Uri.EscapeDataString(token)}");
        }
    }
}
