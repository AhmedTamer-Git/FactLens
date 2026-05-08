namespace Factlens.Core.Models
{
    public class SearchRecord
    {
        public int Id { get; set; }
        public string SearchText { get; set; }
        public string Verdict { get; set; }
        public int ConfidenceScore { get; set; }
        public string Explanation { get; set; }

        public string TopSourcesJson { get; set; }

        public DateTime SearchTime { get; set; } = DateTime.UtcNow;

        public string ShareId { get; set; } = Guid.NewGuid().ToString("N");

        // ✅ text | image | url
        public string SourceType { get; set; } = "text";

        // ✅ Nullable عشان تفضل موجودة لو اليوزر اتمسح
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}