using System.Text.Json.Serialization;

namespace BizBot.WebApi.Data
{
    public class WebhookData
    {
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = default!;

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = default!;

        [JsonPropertyName("customer")]
        public WebhookCustomer Customer { get; set; } = default!;

        // REQUIRED: metadata you sent during initialize
        [JsonPropertyName("metadata")]
        public WebhookMetadata Metadata { get; set; } = default!;

        // REQUIRED: subscription info returned by Paystack
        [JsonPropertyName("subscription")]
        public WebhookSubscription? Subscription { get; set; }
    }

    public class WebhookMetadata
    {
        [JsonPropertyName("plan")]
        public string Plan { get; set; } = default!;

        [JsonPropertyName("billing_cycle")]
        public string BillingCycle { get; set; } = default!;
    }

    public class WebhookSubscription
    {
        [JsonPropertyName("subscription_code")]
        public string SubscriptionCode { get; set; } = default!;

        [JsonPropertyName("email_token")]
        public string EmailToken { get; set; } = default!;
    }

    public class WebhookCustomer
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = default!;
    }
}
