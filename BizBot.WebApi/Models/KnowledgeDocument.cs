using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Text.Json.Serialization;

namespace BizBot.WebApi.Models
{
    // Simplified model for documents with proper attributes
    public class KnowledgeDocument
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [SearchableField(IsFilterable = true, IsSortable = true)]
        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft)]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        [SearchableField(IsFilterable = true)]
        [JsonPropertyName("source")]
        public string Source { get; set; } = "unknown";

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }
}
