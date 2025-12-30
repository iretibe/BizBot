using BizBot.WebApi.Models;
using BizBot.WebApi.Requests;
using BizBot.WebApi.Responses;
using BizBot.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace BizBot.WebApi.Endpoints
{
    public static class ChatEndpoints
    {
        public static void MapChatEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/chat");

            group.RequireRateLimiting("chat-per-tenant");

            group.MapPost("/completion", async (
                [FromBody] ChatRequest request,
                [FromServices] OpenAIService openAiService,
                [FromServices] PaystackService paystack,
                [FromServices] CosmosDbService cosmos,
                HttpContext context,
                IConfiguration config,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var logger = loggerFactory.CreateLogger("ChatCompletion");

                    // Validate widget token
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(authHeader))
                        return Results.Unauthorized();

                    var token = authHeader.Replace("Bearer ", "");

                    var handler = new JwtSecurityTokenHandler();
                    var principal = handler.ValidateToken(
                        token,
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidIssuer = "bizbot",
                            ValidAudience = "bizbot-widget",
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(config["Widget:SigningKey"]!))
                        },
                        out _);

                    var tenantId = principal.FindFirst("tid")?.Value;
                    if (string.IsNullOrWhiteSpace(tenantId))
                        return Results.Unauthorized();

                    // Load tenant
                    var tenant = await cosmos.GetTenantByIdAsync(tenantId);
                    if (tenant == null || !tenant.IsActive)
                        return Results.Forbid();

                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    var usage = await cosmos.GetDailyUsageAsync(tenant.Id, today);

                    // Enforce daily message limit
                    var dailyLimit = PlanLimits.DailyMessages(tenant.Plan!);

                    if (usage.Messages >= dailyLimit)
                    {
                        return Results.Problem(
                            title: "Usage limit reached",
                            detail: "Upgrade your plan to continue chatting",
                            statusCode: 429
                        );
                    }

                    // Near-limit warning (non-blocking)
                    if (PlanLimits.IsNearDailyLimit(usage.Messages, tenant.Plan!))
                    {
                        context.Response.Headers["X-Usage-Warning"] =
                            "You are nearing your daily message limit";
                    }

                    // Conversation limit (new conversations only)
                    if (string.IsNullOrWhiteSpace(request.ConversationId))
                    {
                        var conversationCount =
                            await cosmos.CountConversationsAsync(tenant.Id);

                        if (conversationCount >=
                            PlanLimits.MaxConversations(tenant.Plan!))
                        {
                            return Results.Problem(
                                title: "Conversation limit reached",
                                detail: "Delete old conversations or upgrade",
                                statusCode: 403
                            );
                        }
                    }

                    // AI call
                    var aiResponse = await openAiService.GetCompletionAsync(
                        request.Message,
                        tenant.Id,
                        request.ConversationId,
                        cancellationToken);

                    // Increment usage AFTER success
                    usage.Messages++;
                    await cosmos.UpsertDailyUsageAsync(usage);

                    // Overage billing (hard threshold example)
                    if (usage.Messages > dailyLimit * 30)
                    {
                        // NOTE: stub until you add real invoice logic
                        logger.LogWarning(
                            "Tenant {TenantId} exceeded hard usage threshold",
                            tenant.Id);
                    }

                    // White-label enforcement
                    var finalMessage = aiResponse.Message;

                    if (!PlanLimits.CanUseWhiteLabel(tenant.Plan!))
                    {
                        finalMessage += "\n\n— Powered by BizBot";
                    }

                    return Results.Ok(new ChatResponse(
                        finalMessage,
                        aiResponse.ConversationId
                    ));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });


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