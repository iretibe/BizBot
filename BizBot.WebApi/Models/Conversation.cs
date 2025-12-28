using System.Text.Json.Serialization;

namespace BizBot.WebApi.Models
{
    public class Conversation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("conversationId")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("userMessage")]
        public string UserMessage { get; set; } = string.Empty;

        [JsonPropertyName("assistantMessage")]
        public string AssistantMessage { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
