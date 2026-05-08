namespace Factlens.Core.DTOs
{
    public class SharedResultDto
    {
        public string Claim { get; set; }
        public string? Verdict { get; set; }
        public int? Confidence { get; set; }
        public string? Explanation { get; set; }
        public Dictionary<string, string> Sources { get; set; }
        public DateTime Time { get; set; }
    }
}
