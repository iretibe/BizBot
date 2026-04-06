using BizBot.Client.Responses;

namespace BizBot.Client.Services
{
    public class SearchClientService
    {
        private readonly HttpClient _http;

        public SearchClientService(HttpClient http) => _http = http;

        public async Task<List<SearchResultItem>> SearchAsync(string tenantId, string query)
        {
            var url = $"api/search/search/{tenantId}?query={Uri.EscapeDataString(query)}";
            var resp = await _http.GetFromJsonAsync<SearchResponse>(url);
            
            return resp?.Results ?? new();
        }

        public async Task<bool> IndexAsync(string tenantId, string content, string title)
        {
            var doc = new 
            { 
                TenantId = tenantId, 
                DocumentId = Guid.NewGuid().ToString(), 
                Content = content, 
                Title = title 
            };

            var response = await _http.PostAsJsonAsync("api/search/index", doc);
            
            return response.IsSuccessStatusCode;
        }
    }
}
