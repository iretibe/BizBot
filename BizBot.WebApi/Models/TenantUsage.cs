namespace BizBot.WebApi.Models
{
    public class TenantUsage
    {
        public string TenantId { get; set; } = default!;
        public int MessagesUsed { get; set; }
        public int SearchesUsed { get; set; }
        public DateTime PeriodStart { get; set; }
        public int Tokens { get; set; }
        public decimal CostUsd { get; set; }
    }
}
