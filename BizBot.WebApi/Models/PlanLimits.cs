namespace BizBot.WebApi.Models
{
    public record PlanLimits(int MonthlyMessages, int MonthlySearches);

    public static class PlanLimitMap
    {
        public static PlanLimits Get(string plan) => plan switch
        {
            "starter" => new(1000, 500),
            "pro" => new(10000, 5000),
            "agency" => new(50000, 20000),
            _ => new(0, 0)
        };
    }
}
