namespace BizBot.WebApi.Responses
{
    public class ExchangeRateResponse
    {
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
