namespace Factlens.Core.Models
{
    public class FeedbackRecord
    {
        public int Id { get; set; }

        public int SearchRecordId { get; set; }
        public SearchRecord SearchRecord { get; set; }

        // ✅ Nullable عشان تفضل موجودة لو اليوزر اتمسح
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public bool Helpful { get; set; }
        public int? Rating { get; set; } // 1..5
        public bool ReportIncorrect { get; set; }
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}