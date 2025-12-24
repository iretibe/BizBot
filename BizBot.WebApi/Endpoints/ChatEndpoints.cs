using BizBot.WebApi.Requests;
using BizBot.WebApi.Responses;
using BizBot.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BizBot.WebApi.Endpoints
{
    public static class ChatEndpoints
    {
        public static void MapChatEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/chat");

            group.MapPost("/completion", async ([FromBody] ChatRequest request,
                [FromServices] OpenAIService openAiService,
                HttpContext context, ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var logger = loggerFactory.CreateLogger("ChatCompletion");

                    var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                    if (string.IsNullOrEmpty(tenantId))
                        return Results.BadRequest("Tenant ID is required");

                    logger.LogInformation("TenantId: {TenantId}", tenantId);

                    var response = await openAiService.GetCompletionAsync(
                        request.Message, tenantId, request.ConversationId, cancellationToken);

                    logger.LogInformation("Chat completion returned");

                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetChatCompletion")
            .Produces<ChatResponse>(200)
            .ProducesProblem(400);

            group.MapGet("/history/{tenantId}/{conversationId}", async (
                string tenantId,
                string conversationId,
                [FromServices] CosmosDbService cosmosService) =>
            {
                var history = await cosmosService.GetConversationHistoryAsync(
                    tenantId, conversationId);
                
                return Results.Ok(history);
            });
        }
    }
}