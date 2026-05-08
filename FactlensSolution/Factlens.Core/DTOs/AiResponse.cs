using System.Text.Json.Serialization;

namespace Factlens.Core.DTOs
{
    public class AiResponse
    {
        [JsonPropertyName("verdict")]
        public string Verdict { get; set; }

        [JsonPropertyName("confidence_score")]
        public int ConfidenceScore { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; }

        [JsonPropertyName("top_sources")]
        public Dictionary<string, string> TopSources { get; set; }
    }
}
