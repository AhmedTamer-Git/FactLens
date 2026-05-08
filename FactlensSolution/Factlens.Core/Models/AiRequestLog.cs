namespace Factlens.Core.Models
{
    public class AiRequestLog
    {
        public int Id { get; set; }

        // ✅ Nullable عشان تفضل موجودة لو اليوزر اتمسح
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string InputText { get; set; }
        public string? Verdict { get; set; }
        public int? ConfidenceScore { get; set; }

        public int StatusCode { get; set; }
        public int DurationMs { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}