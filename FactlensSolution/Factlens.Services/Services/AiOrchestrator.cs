using Factlens.Core.DTOs;
using Factlens.Core.Models;
using Factlens.Data.Context;
using Factlens.Services.Interfaces;
using System.Diagnostics;
using System.Text.Json;

namespace Factlens.Services.Services
{
    public class AiOrchestrator : IAiOrchestrator
    {
        private readonly AiService _aiService;
        private readonly AppDbContext _context;

        public AiOrchestrator(AiService aiService, AppDbContext context)
        {
            _aiService = aiService;
            _context = context;
        }

        // ✅ Text / URL
        public async Task<AiResponse> CheckNewsAndSaveAsync(string userId, string text)
        {
            // تحديد SourceType تلقائياً
            var sourceType = text.StartsWith("http://") || text.StartsWith("https://")
                ? "url"
                : "text";

            return await RunAndSaveAsync(
                userId,
                inputText: text,
                sourceType: sourceType,
                aiCall: () => _aiService.CheckNewsAsync(text)
            );
        }

        // ✅ Image
        public async Task<AiResponse> CheckNewsImageAndSaveAsync(
            string userId, Stream imageStream, string fileName)
        {
            // النص اللي هيتخزن في SearchRecord هو اسم الملف كـ placeholder
            // الـ OCR والنص الحقيقي بيتعمل في FastAPI
            var inputLabel = $"[Image: {fileName}]";

            return await RunAndSaveAsync(
                userId,
                inputText: inputLabel,
                sourceType: "image",
                aiCall: () => _aiService.CheckNewsImageAsync(imageStream, fileName)
            );
        }

        // ✅ الـ Core Logic مشترك بين النوعين
        private async Task<AiResponse> RunAndSaveAsync(
            string userId,
            string inputText,
            string sourceType,
            Func<Task<AiResponse>> aiCall)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var aiResponse = await aiCall();
                sw.Stop();

                // Log (Admin Dashboard)
                _context.AiRequestLogs.Add(new AiRequestLog
                {
                    UserId = userId,
                    InputText = inputText.Length > 500 ? inputText[..500] : inputText,
                    Verdict = aiResponse.Verdict,
                    ConfidenceScore = aiResponse.ConfidenceScore,
                    StatusCode = 200,
                    DurationMs = (int)sw.ElapsedMilliseconds,
                    ErrorMessage = null
                });

                // History (SearchRecord)
                _context.SearchRecords.Add(new SearchRecord
                {
                    SearchText = inputText,
                    Verdict = aiResponse.Verdict,
                    ConfidenceScore = aiResponse.ConfidenceScore,
                    Explanation = aiResponse.Explanation,
                    TopSourcesJson = JsonSerializer.Serialize(aiResponse.TopSources),
                    SearchTime = DateTime.UtcNow,
                    SourceType = sourceType,
                    UserId = userId
                });

                await _context.SaveChangesAsync();

                return aiResponse;
            }
            catch (Exception ex)
            {
                sw.Stop();

                _context.AiRequestLogs.Add(new AiRequestLog
                {
                    UserId = userId,
                    InputText = inputText.Length > 500 ? inputText[..500] : inputText,
                    Verdict = null,
                    ConfidenceScore = null,
                    StatusCode = 500,
                    DurationMs = (int)sw.ElapsedMilliseconds,
                    ErrorMessage = ex.Message
                });

                await _context.SaveChangesAsync();
                throw;
            }
        }
    }
}