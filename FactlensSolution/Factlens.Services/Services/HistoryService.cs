using Factlens.Core.DTOs;
using Factlens.Data.Context;
using Factlens.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Factlens.Services.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly AppDbContext _context;

        public HistoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetAsync(
            string userId,
            string? search,
            string? verdict,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize)
        {
            // ✅ حماية من قيم غلط
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;

            var query = _context.SearchRecords
                .AsNoTracking()
                .Where(r => r.UserId == userId);

            // ✅ فلترة بالبحث
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.SearchText.Contains(search));

            // ✅ فلترة بالـ verdict
            if (!string.IsNullOrWhiteSpace(verdict))
                query = query.Where(r => r.Verdict == verdict);

            // ✅ فلترة بالتاريخ
            if (from.HasValue)
                query = query.Where(r => r.SearchTime >= from.Value);

            if (to.HasValue)
                query = query.Where(r => r.SearchTime <= to.Value);

            var totalCount = await query.CountAsync();

            var rows = await query
                .OrderByDescending(r => r.SearchTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.SearchText,
                    r.Verdict,
                    r.ConfidenceScore,
                    r.Explanation,
                    r.TopSourcesJson,
                    r.SearchTime,
                    r.ShareId
                })
                .ToListAsync();

            // ✅ تحويل TopSourcesJson → Dictionary
            var data = rows.Select(r => new HistoryRecordDto
            {
                Id = r.Id,
                SearchText = r.SearchText,
                Verdict = r.Verdict,
                ConfidenceScore = r.ConfidenceScore,
                Explanation = r.Explanation,
                TopSources = string.IsNullOrWhiteSpace(r.TopSourcesJson)
                    ? new Dictionary<string, string>()
                    : (JsonSerializer.Deserialize<Dictionary<string, string>>(r.TopSourcesJson) ?? new Dictionary<string, string>()),
                SearchTime = r.SearchTime,
                ShareId = r.ShareId
            }).ToList();

            return new
            {
                totalCount,
                page,
                pageSize,
                data
            };
        }

        public async Task DeleteAsync(string userId, int id)
        {
            var record = await _context.SearchRecords
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (record == null)
                throw new KeyNotFoundException("العنصر غير موجود أو لا يخص هذا المستخدم.");

            _context.SearchRecords.Remove(record);
            await _context.SaveChangesAsync();
        }

        public async Task ClearAsync(string userId)
        {
            var records = await _context.SearchRecords
                .Where(r => r.UserId == userId)
                .ToListAsync();

            _context.SearchRecords.RemoveRange(records);
            await _context.SaveChangesAsync();
        }
    }
}
