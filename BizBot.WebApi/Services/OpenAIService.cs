using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace BizBot.WebApi.Services
{
    public class OpenAIService
    {
        private readonly AzureOpenAIClient _client;
        private readonly CosmosDbService _cosmos;
        private readonly AzureAISearchService _search;
        private readonly IConfiguration _config;
        private readonly string _deploymentName;

        public OpenAIService(IConfiguration config, 
            CosmosDbService cosmos, AzureAISearchService search)
        {
            _config = config;
            _cosmos = cosmos;
            _search = search;

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
            // Load tenant configuration
            var tenant = await _cosmos.GetTenantConfigAsync(tenantId);
            string deployment = tenant.Model ?? _deploymentName;

            // Retrieve knowledge context (RAG)
            var knowledgeContext = await _search.SearchRelevantContextSimpleAsync(userMessage, tenantId);

            // Build strict system prompt (anti-hallucination)
            var systemPrompt = $"""
                You are BizBot, a helpful AI assistant for a business.

                Use ONLY the knowledge provided below to answer.
                If the answer is not contained in the knowledge, say:
                "I don't have that information yet."

                --- KNOWLEDGE START ---
                {knowledgeContext}
                --- KNOWLEDGE END ---
                """
            ;

            // Create the abstracted client
            IChatClient chatClient = _client.GetChatClient(deployment).AsIChatClient();

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userMessage)
            };

            var options = new ChatOptions
            {
                MaxOutputTokens = tenant.MaxTokens,
                Temperature = 0.2f // factual, RAG-friendly
            };

            // Call OpenAI with controlled timeout
            using var openAiCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            var response = await chatClient.GetResponseAsync(
                messages, options, openAiCts.Token);

            // Extracted response and log conversation
            var assistantMessage = response.Messages.FirstOrDefault()?.Text ?? "I don’t have that information yet.";
            var finalConversationId = conversationId ?? Guid.NewGuid().ToString();

            // Persist conversation
            await _cosmos.LogConversationAsync(tenantId, 
                finalConversationId, userMessage, assistantMessage);

            return new Responses.ChatResponse(assistantMessage, finalConversationId);
        }
    }
}
