namespace BizBot.WebApi.Responses
{
    public class PaystackCustomerResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public CustomerData Data { get; set; } = new();

        public class CustomerData
        {
            public string CustomerCode { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}
