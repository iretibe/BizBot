using System.Text.Json.Serialization;

namespace BizBot.WebApi.Responses
{
    public class PaystackTransactionResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public TransactionData Data { get; set; } = new();

        public class TransactionData
        {
            [JsonPropertyName("authorization_url")]
            public string AuthorizationUrl { get; set; } = string.Empty;

            [JsonPropertyName("access_code")]
            public string AccessCode { get; set; } = string.Empty;
            
            [JsonPropertyName("reference")]
            public string Reference { get; set; } = string.Empty;
        }
    }
}
