namespace BizBot.WebApi.Requests
{
    public record InitializeSubscriptionRequest(
        string PlanName,
        string CustomerEmail,
        string CustomerName,
        string? Phone = null);
}
