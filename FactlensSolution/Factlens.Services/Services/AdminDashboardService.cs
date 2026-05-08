using Factlens.Data.Context;
using Factlens.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Factlens.Services.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly AppDbContext _context;

        public AdminDashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> SummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var last7 = today.AddDays(-7);

            var total = await _context.AiRequestLogs.CountAsync();
            var todayCount = await _context.AiRequestLogs.CountAsync(x => x.CreatedAt >= today);
            var weekCount = await _context.AiRequestLogs.CountAsync(x => x.CreatedAt >= last7);

            var errorCount = await _context.AiRequestLogs.CountAsync(x => x.StatusCode >= 400 && x.CreatedAt >= last7);
            var avgMs = await _context.AiRequestLogs
                .Where(x => x.CreatedAt >= last7)
                .Select(x => (int?)x.DurationMs)
                .AverageAsync() ?? 0;

            return new
            {
                totalRequests = total,
                requestsToday = todayCount,
                requestsLast7Days = weekCount,
                errorsLast7Days = errorCount,
                avgResponseMsLast7Days = (int)avgMs
            };
        }

        public async Task<object> RequestsAsync(int page, int pageSize, int? status, string? search)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 50;

            var q = _context.AiRequestLogs.AsNoTracking();

            if (status.HasValue)
                q = q.Where(x => x.StatusCode == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(x =>
                    x.InputText.Contains(search) ||
                    (x.ErrorMessage != null && x.ErrorMessage.Contains(search)) ||
                    (x.UserId != null && x.UserId.Contains(search)) ||
                    (x.Verdict != null && x.Verdict.Contains(search))
                );

            var total = await q.CountAsync();

            var data = await q.OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    x.InputText,
                    x.Verdict,
                    x.ConfidenceScore,
                    x.StatusCode,
                    x.DurationMs,
                    x.ErrorMessage,
                    x.CreatedAt
                })
                .ToListAsync();

            return new { totalCount = total, page, pageSize, data };
        }

        public async Task<object> VerdictsAsync()
        {
            var last30 = DateTime.UtcNow.Date.AddDays(-30);

            var data = await _context.AiRequestLogs.AsNoTracking()
                .Where(x => x.CreatedAt >= last30 && x.StatusCode == 200 && x.Verdict != null)
                .GroupBy(x => x.Verdict)
                .Select(g => new { verdict = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            return data;
        }

        public async Task<object> FeedbackAsync(int page, int pageSize, bool? reportedOnly)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 50;

            var q = _context.FeedbackRecords.AsNoTracking();

            // ✅ لو الأدمن عايز يشوف البلاغات فقط
            if (reportedOnly == true)
                q = q.Where(f => f.ReportIncorrect);

            var total = await q.CountAsync();

            var data = await q.OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.Id,
                    f.UserId,
                    f.SearchRecordId,
                    f.Helpful,
                    f.Rating,
                    f.ReportIncorrect,
                    f.Comment,
                    f.CreatedAt
                })
                .ToListAsync();

            return new { totalCount = total, page, pageSize, data };
        }
    }
}
