using System.Text.Json.Serialization;

namespace BizBot.WebApi.Responses
{
    public class PaystackVerifyResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;

        [JsonPropertyName("data")]
        public VerifyData Data { get; set; } = default!;

        public class VerifyData
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = default!;

            [JsonPropertyName("reference")]
            public string Reference { get; set; } = default!;

            [JsonPropertyName("amount")]
            public int Amount { get; set; }

            [JsonPropertyName("currency")]
            public string Currency { get; set; } = default!;

            [JsonPropertyName("customer")]
            public Customer Customer { get; set; } = default!;

            [JsonPropertyName("metadata")]
            public Metadata Metadata { get; set; } = default!;
        }

        public class Metadata
        {
            [JsonPropertyName("plan")]
            public string Plan { get; set; } = default!;

            [JsonPropertyName("billing_cycle")]
            public string BillingCycle { get; set; } = default!;
        }

        public class Customer
        {
            [JsonPropertyName("email")]
            public string Email { get; set; } = default!;
        }

        //public class Customer
        //{
        //    public string Email { get; set; } = string.Empty;
        //    public string FirstName { get; set; } = string.Empty;
        //    public string LastName { get; set; } = string.Empty;
        //}

        public class Plan
        {
            public string PlanCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}
