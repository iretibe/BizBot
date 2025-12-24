using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace BizBot.WebApi.Services
{
    public class OpenAIService
    {
        private readonly AzureOpenAIClient _client;
        private readonly CosmosDbService _cosmos;
        private readonly IConfiguration _config;
        private readonly string _deploymentName;

        public OpenAIService(IConfiguration config, CosmosDbService cosmos)
        {
            _config = config;
            _cosmos = cosmos;
            _deploymentName = config["AzureOpenAI:DeploymentName"]!;

            _client = new AzureOpenAIClient(
                new Uri(config["AzureOpenAI:Endpoint"]!),
                new AzureKeyCredential(config["AzureOpenAI:ApiKey"]!)
            );
        }

        public async Task<Responses.ChatResponse> GetCompletionAsync(
            string userMessage, string tenantId, string? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            var tenant = await _cosmos.GetTenantConfigAsync(tenantId);

            string deployment = tenant.Model ?? _deploymentName;

            // Create the abstracted client
            IChatClient chatClient = _client.GetChatClient(deployment).AsIChatClient();

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, tenant.SystemPrompt ?? "You are a helpful assistant."),
                new(ChatRole.User, userMessage)
            };

            var options = new ChatOptions
            {
                MaxOutputTokens = tenant.MaxTokens,
                Temperature = 0.7f
            };

            //var options = new OpenAI.Chat.ChatCompletionOptions()
            //{
            //    MaxOutputTokenCount = tenant.MaxTokens,
            //    Temperature = 0.7f
            //};

            //// New messages for this request
            //var messages = new List<ChatMessage>()
            //{
            //    new ChatMessage(ChatRole.System, "You are a helpful AI assistant."),

            //    new ChatMessage(ChatRole.User, userMessage),
            //};

            //// Azure AI SDK method
            //var response = await _client.GetChatClient(deployment)
            //    .CompleteChatAsync((IList<OpenAI.Chat.ChatMessage>)messages, options, cancellationToken);
                        
            // Call GetResponseAsync (the abstracted method)
            //var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);
            using var openAiCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            var response = await chatClient.GetResponseAsync(
                messages,
                options,
                openAiCts.Token
            );
            //string assistantMessage = response.Value.Content[0].Text;

            //string finalConversationId = conversationId ?? Guid.NewGuid().ToString();

            string assistantMessage = response.Messages.FirstOrDefault()?.Text ?? string.Empty;
            string finalConversationId = conversationId ?? Guid.NewGuid().ToString();

            await _cosmos.LogConversationAsync(tenantId, finalConversationId,
                userMessage, assistantMessage
            );

            return new Responses.ChatResponse(assistantMessage, finalConversationId);
        }
    }
}
