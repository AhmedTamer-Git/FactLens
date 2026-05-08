namespace Factlens.Core.DTOs
{
    public class HistoryRecordDto
    {
        public int Id { get; set; }
        public string SearchText { get; set; }
        public string Verdict { get; set; }
        public int ConfidenceScore { get; set; }
        public string Explanation { get; set; }
        public Dictionary<string, string> TopSources { get; set; }
        public DateTime SearchTime { get; set; }
        public string ShareId { get; set; }
    }
}
