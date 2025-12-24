namespace BizBot.WebApi.Responses
{
    public class PaystackVerifyResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public VerifyData Data { get; set; } = new();

        public class VerifyData
        {
            public string Reference { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty; // "success", "failed"
            public decimal Amount { get; set; }
            public Customer Customer { get; set; } = new();
            public Plan Plan { get; set; } = new();
        }

        public class Customer
        {
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
        }

        public class Plan
        {
            public string PlanCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}
