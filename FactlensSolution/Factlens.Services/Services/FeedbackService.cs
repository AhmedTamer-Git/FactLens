using Factlens.Core.DTOs;
using Factlens.Core.Models;
using Factlens.Data.Context;
using Factlens.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Factlens.Services.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly AppDbContext _context;

        public FeedbackService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(string userId, FeedbackRequest request)
        {
            // ✅ تحقق سريع
            if (request == null || request.SearchRecordId <= 0)
                throw new ArgumentException("SearchRecordId مطلوب.");

            if (request.Rating.HasValue && (request.Rating < 1 || request.Rating > 5))
                throw new ArgumentException("Rating لازم يكون من 1 إلى 5.");

            // ✅ لازم يكون السجل موجود وملك المستخدم (عشان ما يقيمش حاجة مش بتاعته)
            var owns = await _context.SearchRecords
                .AsNoTracking()
                .AnyAsync(r => r.Id == request.SearchRecordId && r.UserId == userId);

            if (!owns)
                throw new KeyNotFoundException("النتيجة غير موجودة أو لا تخص هذا المستخدم.");

            // ✅ منع تكرار الفيدباك على نفس السجل
            var already = await _context.FeedbackRecords
                .AnyAsync(f => f.SearchRecordId == request.SearchRecordId && f.UserId == userId);

            if (already)
                throw new InvalidOperationException("تم إرسال فيدباك على هذه النتيجة من قبل.");

            var feedback = new FeedbackRecord
            {
                SearchRecordId = request.SearchRecordId,
                UserId = userId,
                Helpful = request.Helpful,
                Rating = request.Rating,
                ReportIncorrect = request.ReportIncorrect,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.FeedbackRecords.Add(feedback);
            await _context.SaveChangesAsync();
        }
    }
}
