namespace BizBot.Client.Responses
{
    public record SubscriptionInitResponse(string AuthorizationUrl, string Reference, string AccessCode);
}
