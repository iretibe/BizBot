using System.Text.Json.Serialization;

namespace BizBot.WebApi.Models
{
    public class TenantConfig
    {
        // Identity
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        public string Name { get; set; } = "Default Tenant";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Email { get; set; }


        // Subscription Details
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Plan { get; set; }              // starter | pro | agency

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BillingCycle { get; set; }      // monthly | yearly

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PlanCode { get; set; }           // Paystack plan code


        // PayStack Payment Details
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SubscriptionCode { get; set; }  // For cancel / upgrade

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EmailToken { get; set; }         // Required by Paystack


        // Billing Info
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Amount { get; set; }            // GHS charged

        public bool IsActive { get; set; } = true;

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? CancelledAt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PaymentReference { get; set; }


        // AI Configuration
        public string SystemPrompt { get; set; } = "You are a helpful AI assistant for a business website.";

        public string Model { get; set; } = "bizbot-chat";
        public int MaxTokens { get; set; } = 800;

        public WhiteLabelSettings? WhiteLabelSettings { get; set; }

        public string? CustomSystemPrompt { get; set; }
    }
}
