using BizBot.WebApi.Data;
using BizBot.WebApi.Endpoints;

namespace BizBot.WebApi.Events
{
    public class PaystackWebhookEvent
    {
        public string Event { get; set; } = string.Empty; // "charge.success", "subscription.not_renew"
        public WebhookData Data { get; set; } = new();
    }
}
