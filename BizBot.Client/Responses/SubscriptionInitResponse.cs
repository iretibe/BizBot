using System.Text.Json.Serialization;

namespace BizBot.Client.Responses
{
    public record SubscriptionInitResponse(
        [property: JsonPropertyName("authorizationUrl")]
        string AuthorizationUrl,

        [property: JsonPropertyName("reference")]
        string Reference,

        [property: JsonPropertyName("accessCode")]
        string AccessCode
    );
}
