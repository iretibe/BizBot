using BizBot.WebApi.Events;
using BizBot.WebApi.Services;
using System.Text.Json;

namespace BizBot.WebApi.Endpoints
{
    public static class PaystackEndpoint
    {
        public static void MapPaystackEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/paystack");

            app.MapPost("/api/paystack/webhook", async (
                HttpRequest request,
                PaystackService paystack,
                CosmosDbService cosmos,
                ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("PaystackWebhook");

                // 1️⃣ Read raw body
                using var reader = new StreamReader(request.Body);
                var payload = await reader.ReadToEndAsync();

                // 2️⃣ Validate signature
                if (!request.Headers.TryGetValue("x-paystack-signature", out var signature))
                {
                    logger.LogWarning("Missing Paystack signature");
                    return Results.Unauthorized();
                }

                if (!paystack.ValidateWebhookSignature(payload, signature!))
                {
                    logger.LogWarning("Invalid Paystack signature");
                    return Results.Unauthorized();
                }

                // Deserialize after validation
                var webhook = JsonSerializer.Deserialize<PaystackWebhookEvent>(payload);
                if (webhook == null)
                    return Results.BadRequest();

                // Idempotency check
                var processed = await cosmos.HasProcessedWebhookAsync(webhook.Event, webhook.Data.Reference);
                if (processed)
                {
                    logger.LogInformation("Duplicate webhook ignored: {Ref}", webhook.Data.Reference);
                    return Results.Ok();
                }

                // Handle event
                if (webhook.Event == "charge.success")
                {
                    await cosmos.MarkSubscriptionPaidAsync(webhook.Data.Reference);
                }

                // Mark processed
                await cosmos.MarkWebhookProcessedAsync(webhook.Event, webhook.Data.Reference);

                return Results.Ok();
            });
        }
    }
}
