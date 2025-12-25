using BizBot.Client.Responses;

namespace BizBot.Client.Services
{
    public class SubscriptionClientService
    {
        private readonly HttpClient _http;

        public SubscriptionClientService(HttpClient http) => _http = http;

        public async Task<SubscriptionInitResponse?> InitializeAsync(string plan, string email, string name)
        {
            var request = new 
            { 
                PlanName = plan, 
                CustomerEmail = email, 
                CustomerName = name 
            };

            var response = await _http.PostAsJsonAsync("api/subscriptions/initialize", request);
            
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<SubscriptionInitResponse>();
        }
    }
}
