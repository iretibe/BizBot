namespace BizBot.WebApi.Responses
{
    public class PaystackTransactionResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public TransactionData Data { get; set; } = new();

        public class TransactionData
        {
            public string AuthorizationUrl { get; set; } = string.Empty;
            public string AccessCode { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
        }
    }
}
