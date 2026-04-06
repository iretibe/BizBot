namespace BizBot.WebApi.Models
{
    public class ProcessedWebhook
    {
        public string Id { get; set; } = default!; // event_reference
        public string EventType { get; set; } = default!;
        public string Reference { get; set; } = default!;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
