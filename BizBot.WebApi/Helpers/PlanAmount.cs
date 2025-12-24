namespace BizBot.WebApi.Helpers
{
    public static class PlanAmount
    {
        public static decimal GetPlanAmount(string planName)
        {
            return planName.ToLower() switch
            {
                "starter" => 29.00m,
                "pro" => 99.00m,
                "agency" => 299.00m,
                _ => throw new ArgumentException($"Unknown plan: {planName}")
            };
        }
    }
}
