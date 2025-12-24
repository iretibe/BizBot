namespace BizBot.WebApi.Data
{
    public class WebhookData
    {
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public CustomerData? Customer { get; set; }
        public PlanData? Plan { get; set; }
    }
}
