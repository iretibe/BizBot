namespace BizBot.WebApi.Interfaces
{
    public interface IExchangeRateService
    {
        Task<decimal> ConvertUsdToGhsAsync(decimal usdAmount);
    }
}
