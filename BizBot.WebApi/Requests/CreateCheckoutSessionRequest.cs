namespace BizBot.WebApi.Requests
{
    public record CreateCheckoutSessionRequest(string PriceId,
        string CustomerEmail, string SuccessUrl, string CancelUrl);
}
