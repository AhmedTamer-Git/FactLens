using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factlens.Services.Services
{
    public static class EmailTemplateHelper
    {
        /// <summary>
        /// بيرجع HTML الإيميل مع الـ confirmation link متحطة فيه
        /// </summary>
        public static string GetConfirmationEmail(string confirmationLink)
        {
            // ── اقرأ الـ template من ملف ──
            // الملف المفروض يكون في: wwwroot/email-templates/email-confirm-template.html
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "email-templates", "email-confirm-template.html"
            );

            string template;

            if (File.Exists(templatePath))
            {
                template = File.ReadAllText(templatePath);
            }
            else
            {
                // Fallback: inline minimal HTML لو الملف مش موجود
                template = GetFallbackTemplate();
            }

            // ── استبدل الـ placeholder بالـ link الحقيقي ──
            return template.Replace("{CONFIRMATION_LINK}", confirmationLink);
        }

        /// <summary>
        /// بيرجع HTML إيميل الـ reset password مع الـ reset link متحطة فيه
        /// </summary>
        public static string GetResetEmail(string resetLink)
        {
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "email-templates", "email-reset-template.html"
            );

            string template;

            if (File.Exists(templatePath))
            {
                template = File.ReadAllText(templatePath);
            }
            else
            {
                template = GetFallbackResetTemplate();
            }

            return template.Replace("{RESET_LINK}", resetLink);
        }

        // ── Fallback بسيط لو الملف مش موجود ──
        private static string GetFallbackTemplate()
        {
            return @"<!DOCTYPE html>
<html><head><meta charset='UTF-8'/></head>
<body style='font-family:sans-serif;background:#F0F2F5;padding:40px 20px;'>
  <div style='max-width:520px;margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;'>
    <div style='background:#1A305B;padding:32px;text-align:center;'>
      <div style='font-size:28px;font-weight:700;color:#fff;'>Fact<span style=""color:#CDA755"">Lens</span></div>
    </div>
    <div style='padding:40px 32px;text-align:center;'>
      <h1 style='font-size:22px;color:#1A305B;margin-bottom:12px;'>Confirm your Email</h1>
      <p style='color:#78808A;margin-bottom:28px;'>Click below to activate your account.</p>
      <a href='{CONFIRMATION_LINK}' style='background:#CDA755;color:#1A305B;padding:14px 36px;border-radius:8px;font-weight:700;text-decoration:none;font-size:15px;'>Confirm Email</a>
      <p style='margin-top:24px;font-size:12px;color:#9CA3AF;'>Link expires in 24 hours.</p>
    </div>
  </div>
</body></html>";
        }

        // ── Fallback reset password لو الملف مش موجود ──
        private static string GetFallbackResetTemplate()
        {
            return @"<!DOCTYPE html>
<html><head><meta charset='UTF-8'/></head>
<body style='font-family:sans-serif;background:#F0F2F5;padding:40px 20px;'>
  <div style='max-width:520px;margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;'>
    <div style='background:#1A305B;padding:32px;text-align:center;'>
      <div style='font-size:28px;font-weight:700;color:#fff;'>Fact<span style=""color:#CDA755"">Lens</span></div>
    </div>
    <div style='padding:40px 32px;text-align:center;'>
      <h1 style='font-size:22px;color:#1A305B;margin-bottom:12px;'>Reset your Password</h1>
      <p style='color:#78808A;margin-bottom:28px;'>Click below to reset your password.</p>
      <a href='{RESET_LINK}' style='background:#CDA755;color:#1A305B;padding:14px 36px;border-radius:8px;font-weight:700;text-decoration:none;font-size:15px;'>Reset Password</a>
      <p style='margin-top:24px;font-size:12px;color:#9CA3AF;'>Link expires in 1 hour.</p>
    </div>
  </div>
</body></html>";
        }
    }
}
