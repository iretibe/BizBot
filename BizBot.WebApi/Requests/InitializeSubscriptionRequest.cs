namespace BizBot.WebApi.Requests
{
    public record InitializeSubscriptionRequest(
        string PlanName,
        string BillingCycle, // "monthly" | "yearly"
        string CustomerEmail,
        string CustomerName);
        //string? Phone = null);
}
