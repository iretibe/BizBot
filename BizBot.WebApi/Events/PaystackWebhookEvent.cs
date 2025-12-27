using BizBot.WebApi.Data;
using System.Text.Json.Serialization;

namespace BizBot.WebApi.Events
{
    public class PaystackWebhookEvent
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = default!;

        [JsonPropertyName("data")]
        public WebhookData Data { get; set; } = default!;
    }
}
