namespace BizBot.WebApi.Models
{
    public static class PlanLimits
    {
        public static bool CanUseWhiteLabel(string plan)
            => plan is "pro" or "agency";

        public static int DailyMessages(string plan) => plan switch
        {
            "starter" => 100,
            "pro" => 1000,
            "agency" => int.MaxValue,
            _ => 0
        };

        public static int MonthlyMessageLimit(string plan) => plan switch
        {
            "starter" => 5_000,
            "pro" => 25_000,
            "agency" => 100_000,
            _ => 0
        };

        public static int MaxConversations(string plan) => plan switch
        {
            "starter" => 20,
            "pro" => 200,
            "agency" => int.MaxValue,
            _ => 0
        };

        public static bool IsNearDailyLimit(int used, string plan)
            => used >= DailyMessages(plan) * 0.8;
    }
}
