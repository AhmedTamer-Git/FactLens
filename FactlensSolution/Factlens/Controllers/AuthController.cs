using Factlens.Core.DTOs;
using Factlens.Core.Models;
using Factlens.Services.Interfaces;
using Factlens.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Factlens.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtService _jwtService;
        private readonly IEmailService _emailService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            JwtService jwtService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        // ================= Register =================
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Password and Confirm Password do not match");

            if (await _userManager.FindByNameAsync(dto.Username) != null)
                return BadRequest("Username already exists");

            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return BadRequest("Email already exists");

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                Age = dto.Age,
                Phone = dto.Phone
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // ================= Email Confirmation =================
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var confirmationLink = $"https://factlens.runasp.net/html/confirm-email.html?userId={user.Id}&token={encodedToken}";

            await _emailService.SendEmailAsync(
                 user.Email,
                 "Confirm your FactLens account ✉️",
                  EmailTemplateHelper.GetConfirmationEmail(confirmationLink)
             );

            return Ok(new
            {
                message = "Registration successful. Please check your email to confirm your account."
            });
        }

        // ================= Confirm Email =================
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                return BadRequest(new { message = "Email confirmation failed. The link may have expired." });

            return Ok(new { message = "Email confirmed successfully." });
        }

        // ================= Login =================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null)
                user = await _userManager.FindByEmailAsync(dto.Username);

            if (user == null)
                return BadRequest("User not found");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                return BadRequest("Wrong password");

            // ✅ منع الدخول لو الإيميل مش متأكد
            if (!user.EmailConfirmed)
                return BadRequest("Please confirm your email before signing in.");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user.Id, user.Email, user.UserName, roles);

            return Ok(new
            {
                token,
                email = user.Email,
                username = user.UserName,
                fullName = user.FullName,
                roles
            });
        }

        // ================= Forgot Password =================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Ok();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetLink = $"https://factlens.runasp.net/html/reset_password.html?token={encodedToken}&email={user.Email}";
            await _emailService.SendEmailAsync(
                user.Email,
                "Reset your FactLens password 🔑",
                EmailTemplateHelper.GetResetEmail(resetLink)
            );

            return Ok(new { message = "Reset link sent to your email" });
        }

        // ================= Resend Confirmation Email =================
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null && !user.EmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var confirmationLink = $"https://factlens.runasp.net/html/confirm-email.html?userId={user.Id}&token={encodedToken}";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Confirm your FactLens account ✉️",
                    EmailTemplateHelper.GetConfirmationEmail(confirmationLink)
                );
            }

            // دايماً Ok عشان ما نكشفش وجود الإيميل
            return Ok(new { message = "If this email is registered and unconfirmed, a new link has been sent." });
        }

        // ================= Reset Password =================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest("User not found");

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("New password and confirmation do not match");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));

            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                dto.NewPassword
            );

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password reset successfully");
        }
    }
}