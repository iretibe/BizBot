using BizBot.WebApi.Interfaces;
using BizBot.WebApi.Responses;

namespace BizBot.WebApi.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ExchangeRateService(HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<decimal> ConvertUsdToGhsAsync(decimal usdAmount)
        {
            var response = await _httpClient
                .GetFromJsonAsync<ExchangeRateResponse>(_configuration["OpenErApi:ApiUrl"]);

            var rate = response!.Rates["GHS"];

            return Math.Round(usdAmount * rate, 2);
        }
    }
}
