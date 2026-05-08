using Factlens.Core.DTOs;
using Factlens.Data.Context;
using Factlens.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using ImageColor = SixLabors.ImageSharp.Color;

namespace Factlens.Services.Services
{
    public class ShareService : IShareService
    {
        private readonly AppDbContext _context;

        public ShareService(AppDbContext context)
        {
            _context = context;
        }

        // ── Verdict helpers ────────────────────────────────────
        private static (string label, string icon, ImageColor color) GetVerdict(string? verdict)
        {
            return verdict?.ToLower() switch
            {
                "true" or "verified true" => ("Verified True", "✓", ImageColor.ParseHex("#22c55e")),
                "mostly true" => ("Mostly True", "✓", ImageColor.ParseHex("#84cc16")),
                "misleading" => ("Misleading", "!", ImageColor.ParseHex("#f59e0b")),
                "false" => ("False", "✗", ImageColor.ParseHex("#ef4444")),
                _ => ("Unverifiable", "?", ImageColor.ParseHex("#9ca3af")),
            };
        }

        // ==============================
        // Get Shared Result
        // ==============================
        public async Task<SharedResultDto> GetSharedAsync(string shareId)
        {
            var record = await _context.SearchRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ShareId == shareId);

            if (record == null)
                throw new KeyNotFoundException("اللينك غير صحيح أو النتيجة لم تعد موجودة.");

            var sources = string.IsNullOrWhiteSpace(record.TopSourcesJson)
                ? new Dictionary<string, string>()
                : (JsonSerializer.Deserialize<Dictionary<string, string>>(record.TopSourcesJson)
                    ?? new Dictionary<string, string>());

            return new SharedResultDto
            {
                Claim = record.SearchText,
                Verdict = record.Verdict,
                Confidence = record.ConfidenceScore,
                Explanation = record.Explanation,
                Sources = sources,
                Time = record.SearchTime
            };
        }

        // ==============================
        // Export Image PNG  (ImageSharp)
        // 1200 × 675  (16:9)
        // ==============================
        public async Task<byte[]> ExportImageAsync(string shareId)
        {
            var data = await GetSharedAsync(shareId);
            var (verdictLabel, verdictIcon, verdictColor) = GetVerdict(data.Verdict);

            const int W = 1200, H = 675;

            // ── Colours ────────────────────────────────────────
            var navy = ImageColor.ParseHex("#1A305B");
            var gold = ImageColor.ParseHex("#CDA755");
            var cream = ImageColor.ParseHex("#FAF9F6");
            var white = ImageColor.ParseHex("#FFFFFF");
            var grayDark = ImageColor.ParseHex("#374151");
            var grayMid = ImageColor.ParseHex("#78808A");
            var grayLight = ImageColor.ParseHex("#E9ECEF");

            // ── Fonts ──────────────────────────────────────────
            var brandFont = SystemFonts.CreateFont("Arial", 30, FontStyle.Bold);
            var subFont = SystemFonts.CreateFont("Arial", 12, FontStyle.Regular);
            var scoreFont = SystemFonts.CreateFont("Arial", 42, FontStyle.Bold);
            var pctFont = SystemFonts.CreateFont("Arial", 12, FontStyle.Regular);
            var pillFont = SystemFonts.CreateFont("Arial", 16, FontStyle.Bold);
            var labelFont = SystemFonts.CreateFont("Arial", 17, FontStyle.Bold);
            var bodyFont = SystemFonts.CreateFont("Arial", 14, FontStyle.Regular);
            var tinyFont = SystemFonts.CreateFont("Arial", 11, FontStyle.Regular);

            using var image = new Image<Rgba32>(W, H);

            image.Mutate(ctx =>
            {
                // ══════════════════════════════════════════
                // BACKGROUND
                // ══════════════════════════════════════════
                ctx.Fill(cream);

                // ══════════════════════════════════════════
                // HEADER  (0 → 80)
                // ══════════════════════════════════════════
                ctx.Fill(navy, new RectangleF(0, 0, W, 80));
                ctx.Fill(gold, new RectangleF(0, 80, W, 4));

                // Brand
                ctx.DrawText("FactLens", brandFont, white, new PointF(40, 18));
                ctx.DrawText("AI-Powered Fact Checking", subFont, gold, new PointF(40, 56));

                // Date (right-aligned approx)
                var dateStr = $"Generated {data.Time:MMM dd, yyyy  HH:mm}";
                ctx.DrawText(dateStr, tinyFont, ImageColor.ParseHex("#aab4c4"), new PointF(880, 36));

                // ══════════════════════════════════════════
                // LEFT PANEL  x:0→280, y:84→H-50
                // Score circle + verdict pill stacked neatly
                // ══════════════════════════════════════════
                const int panelX = 0;
                const int panelW = 280;

                // subtle left panel bg
                ctx.Fill(ImageColor.ParseHex("#F0EDE8"), new RectangleF(panelX, 84, panelW, H - 134));

                // Circle  — centred at (140, 220)
                const int cx = 140, cy = 220, cr = 80;
                ctx.Fill(white, new EllipsePolygon(cx, cy, cr));
                ctx.Draw(verdictColor, 7f, new EllipsePolygon(cx, cy, cr));

                // Score number  — manual centre
                var scoreStr = $"{data.Confidence}";
                // measure width roughly: 42px font ~25px per digit
                float scoreW = scoreStr.Length * 25f;
                ctx.DrawText(scoreStr, scoreFont, verdictColor,
                    new PointF(cx - scoreW / 2f, cy - 30));

                // "% Credibility" label
                ctx.DrawText("% Credibility", pctFont, grayMid,
                    new PointF(cx - 42, cy + 26));

                // Verdict pill below circle
                // Pill background rect  (rounded via fill + small rects trick — ImageSharp path)
                const int pillY = cy + cr + 18;   // ~318
                const int pillW = 190, pillH = 34;
                int pillX = cx - pillW / 2;
                DrawRoundedRect(ctx, verdictColor, pillX, pillY, pillW, pillH, 17);

                // Pill text  (icon + label)
                var pillText = $"{verdictIcon}  {verdictLabel}";
                float pillTextW = pillText.Length * 9f;
                ctx.DrawText(pillText, pillFont, white,
                    new PointF(pillX + (pillW - pillTextW) / 2f, pillY + 8));

                // Small "Confidence Score" label
                ctx.DrawText("Confidence Score", tinyFont, grayMid,
                    new PointF(cx - 52, cy + cr + 62));

                // ══════════════════════════════════════════
                // RIGHT CONTENT  x:296, y:100
                // ══════════════════════════════════════════
                const int rx = 296;
                int ry = 104;

                // ── Claim box ──────────────────────────────
                const int claimH = 88;
                DrawRoundedRect(ctx, white, rx, ry, W - rx - 40, claimH, 8);
                // Gold left accent
                ctx.Fill(gold, new RectangleF(rx, ry, 4, claimH));
                // Gold border
                DrawRoundedRectBorder(ctx, gold, rx, ry, W - rx - 40, claimH, 8, 1.5f);

                var claimText = $"\"{TruncateText(data.Claim, 130)}\"";
                ctx.DrawText(
                    new RichTextOptions(bodyFont)
                    {
                        Origin = new PointF(rx + 16, ry + 12),
                        WrappingLength = W - rx - 72,
                        LineSpacing = 1.5f
                    },
                    claimText, grayDark);

                ry += claimH + 20;

                // ── Divider ────────────────────────────────
                ctx.Fill(grayLight, new RectangleF(rx, ry, W - rx - 40, 1));
                ry += 14;

                // ── Explanation ────────────────────────────
                ctx.DrawText("AI Explanation", labelFont, navy, new PointF(rx, ry));
                ctx.Fill(gold, new RectangleF(rx, ry + 22, 110, 2));
                ry += 32;

                var expText = TruncateText(data.Explanation ?? "No explanation available.", 300);
                ctx.DrawText(
                    new RichTextOptions(bodyFont)
                    {
                        Origin = new PointF(rx, ry),
                        WrappingLength = W - rx - 40,
                        LineSpacing = 1.6f
                    },
                    expText, grayDark);

                ry += 105;

                // ── Sources ────────────────────────────────
                if (data.Sources != null && data.Sources.Any())
                {
                    ctx.Fill(grayLight, new RectangleF(rx, ry, W - rx - 40, 1));
                    ry += 14;

                    ctx.DrawText("Evidence Sources", labelFont, navy, new PointF(rx, ry));
                    ctx.Fill(gold, new RectangleF(rx, ry + 22, 130, 2));
                    ry += 34;

                    foreach (var source in data.Sources.Take(3))
                    {
                        DrawRoundedRect(ctx, white, rx, ry, W - rx - 40, 30, 6);
                        DrawRoundedRectBorder(ctx, grayLight, rx, ry, W - rx - 40, 30, 6, 1f);
                        ctx.DrawText($"🔗  {TruncateText(source.Key, 80)}", tinyFont, navy,
                            new PointF(rx + 12, ry + 9));
                        ry += 38;
                    }
                }

                // ══════════════════════════════════════════
                // FOOTER  (H-50 → H)
                // ══════════════════════════════════════════
                ctx.Fill(navy, new RectangleF(0, H - 50, W, 50));
                ctx.DrawText("FactLens — Verify before you share", tinyFont, gold,
                    new PointF(40, H - 28));
                ctx.DrawText("factlens.com", tinyFont,
                    ImageColor.ParseHex("#aab4c4"), new PointF(W - 110, H - 28));
            });

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }

        // ── Draw filled rounded rectangle ─────────────────────
        private static void DrawRoundedRect(IImageProcessingContext ctx,
            ImageColor color, int x, int y, int w, int h, int r)
        {
            var path = BuildRoundedRectPath(x, y, w, h, r);
            ctx.Fill(color, path);
        }

        // ── Draw rounded rectangle border only ────────────────
        private static void DrawRoundedRectBorder(IImageProcessingContext ctx,
            ImageColor color, int x, int y, int w, int h, int r, float thickness)
        {
            var path = BuildRoundedRectPath(x, y, w, h, r);
            ctx.Draw(color, thickness, path);
        }

        private static IPath BuildRoundedRectPath(int x, int y, int w, int h, int r)
        {
            var pb = new PathBuilder();
            pb.MoveTo(new PointF(x + r, y));
            pb.LineTo(new PointF(x + w - r, y));
            pb.ArcTo(r, r, 0, false, true, new PointF(x + w, y + r));
            pb.LineTo(new PointF(x + w, y + h - r));
            pb.ArcTo(r, r, 0, false, true, new PointF(x + w - r, y + h));
            pb.LineTo(new PointF(x + r, y + h));
            pb.ArcTo(r, r, 0, false, true, new PointF(x, y + h - r));
            pb.LineTo(new PointF(x, y + r));
            pb.ArcTo(r, r, 0, false, true, new PointF(x + r, y));
            pb.CloseFigure();
            return pb.Build();
        }

        // ── Helper: truncate long text ─────────────────────────
        private static string TruncateText(string? text, int maxLen)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLen ? text : text[..maxLen] + "…";
        }
    }
}