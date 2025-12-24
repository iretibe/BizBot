using BizBot.Client.Enums;
using BizBot.Client.Responses;

namespace BizBot.Client.Services
{
    public class ChatClientService
    {
        private readonly HttpClient _http;

        public ChatClientService(HttpClient http)
        {
            _http = http;
        }

        //public async Task<ChatResponse?> SendMessageAsync(
        //    string tenantId, string message, string? conversationId = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    var request = new
        //    {
        //        Message = message,
        //        ConversationId = conversationId,
        //        Source = ConversationSource.ClientApp
        //    };

        //    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        //    cts.CancelAfter(TimeSpan.FromSeconds(60));

        //    var response = await _http.PostAsJsonAsync(
        //        "api/chat/completion",
        //        request,
        //        cts.Token
        //    );

        //    response.EnsureSuccessStatusCode();

        //    return await response.Content.ReadFromJsonAsync<ChatResponse>();
        //}

        public async Task<ChatResponse?> SendMessageAsync(string tenantId,
            string message, string? conversationId = null, CancellationToken cancellationToken = default)
        {
            _http.DefaultRequestHeaders.Remove("X-Tenant-Id");
            _http.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);

            var request = new
            {
                Message = message,
                ConversationId = conversationId,
                Source = ConversationSource.ClientApp
            };

            // Use a linked token source and cancel after a sensible timeout (works in WASM)
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(5)); // adjust timeout as needed

            //var response = await _http.PostAsJsonAsync("api/chat/completion", request);
            var response = await _http.PostAsJsonAsync("api/chat/completion", request, cts.Token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChatResponse>();
        }

        public async Task<List<ConversationResponse>> GetHistoryAsync(string tenantId, string conversationId)
        {
            return await _http.GetFromJsonAsync<List<ConversationResponse>>(
                $"api/chat/history/{tenantId}/{conversationId}"
            ) ?? new();
        }
    }
}
