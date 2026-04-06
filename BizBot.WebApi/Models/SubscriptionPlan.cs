namespace BizBot.WebApi.Models
{
    public class SubscriptionPlan
    {
        public string Key { get; set; } = default!;

        // Prices shown in UI (USD)
        public decimal MonthlyUsd { get; set; }
        public decimal YearlyUsd { get; set; }

        // Paystack identifiers (GHS-based plans)
        public string MonthlyPlanCode { get; set; } = default!;
        public string YearlyPlanCode { get; set; } = default!;
    }
}
