namespace BizBot.WebApi.Responses
{
    public class PaystackPlanResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlanData Data { get; set; } = new();

        public class PlanData
        {
            public string PlanCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }
    }
}
