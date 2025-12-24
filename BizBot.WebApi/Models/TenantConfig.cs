using System.Text.Json.Serialization;

namespace BizBot.WebApi.Models
{
    public class TenantConfig
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        public string Name { get; set; } = "Default Tenant";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Email { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Plan { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PlanCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Amount { get; set; }

        public bool IsActive { get; set; } = true;

        public string SystemPrompt { get; set; } =
            "You are a helpful AI assistant for a business website.";

        public string Model { get; set; } = "bizbot-chat";
        public int MaxTokens { get; set; } = 800;

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PaymentReference { get; set; }
    }
}
