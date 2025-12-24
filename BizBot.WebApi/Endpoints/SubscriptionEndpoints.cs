using BizBot.WebApi.Events;
using BizBot.WebApi.Helpers;
using BizBot.WebApi.Models;
using BizBot.WebApi.Requests;
using BizBot.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BizBot.WebApi.Endpoints
{
    public static class SubscriptionEndpoints
    {
        public static void MapSubscriptionEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/subscriptions");

            // Initialize subscription/checkout
            group.MapPost("/initialize", async (
                [FromBody] InitializeSubscriptionRequest request,
                [FromServices] PaystackService paystackService) =>
            {
                try
                {
                    // Determine amount based on plan
                    var amount = PlanAmount.GetPlanAmount(request.PlanName);

                    // Generate unique reference
                    var reference = $"BIZBOT_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid():N}";

                    // Create metadata
                    var metadata = new Dictionary<string, string>
                    {
                        ["plan"] = request.PlanName,
                        ["customer_email"] = request.CustomerEmail,
                        ["customer_name"] = request.CustomerName,
                        ["app"] = "BizBot"
                    };

                    // Initialize PayStack transaction
                    var response = await paystackService.CreateSubscriptionAsync(
                        request.PlanName,
                        request.CustomerEmail,
                        amount,
                        reference,
                        metadata);

                    if (!response.Status)
                        return Results.BadRequest(new { error = response.Message });

                    return Results.Ok(new
                    {
                        authorizationUrl = response.Data.AuthorizationUrl,
                        reference = response.Data.Reference,
                        accessCode = response.Data.AccessCode
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error: {ex.Message}");
                }
            })
            .WithName("InitializeSubscription");

            // Verify transaction (callback from PayStack)
            group.MapGet("/verify/{reference}", async (
                string reference,
                [FromServices] PaystackService paystackService,
                [FromServices] CosmosDbService cosmosService) =>
            {
                try
                {
                    var verification = await paystackService.VerifyTransactionAsync(reference);

                    if (verification.Status && verification.Data.Status == "success")
                    {
                        // Create tenant in Cosmos DB
                        var tenant = new TenantConfig
                        {
                            Id = reference,
                            Name = $"{verification.Data.Customer.FirstName} {verification.Data.Customer.LastName}",
                            Email = verification.Data.Customer.Email,
                            Plan = verification.Data.Plan.Name,
                            PlanCode = verification.Data.Plan.PlanCode,
                            Amount = verification.Data.Amount / 100, // Convert back from kobo
                            SubscribedAt = DateTime.UtcNow,
                            IsActive = true,
                            PaymentReference = reference,

                            // AI defaults
                            SystemPrompt = "You are a helpful AI assistant for this business.",
                            Model = "bizbot-chat",
                            MaxTokens = 800
                        };

                        //await cosmosService.CreateTenantAsync(tenant);
                        await cosmosService.UpsertTenantAsync(tenant);

                        // Redirect to success page
                        return Results.Redirect("https://your-app.com/success");
                    }
                    else
                    {
                        // Redirect to failure page
                        return Results.Redirect($"https://your-app.com/failed?error={verification.Message}");
                    }
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Verification error: {ex.Message}");
                }
            })
            .WithName("VerifyTransaction");

            // PayStack webhook endpoint
            group.MapPost("/webhook", async (
                HttpRequest request,
                [FromServices] PaystackService paystackService,
                [FromServices] CosmosDbService cosmosService) =>
            {
                // Read the request body
                var json = await new StreamReader(request.Body).ReadToEndAsync();

                try
                {
                    // Validate webhook signature
                    var signature = request.Headers["x-paystack-signature"].FirstOrDefault();
                    if (!paystackService.ValidateWebhookSignature(json, signature!))
                        return Results.Unauthorized();

                    // Parse webhook event
                    var webhookEvent = JsonSerializer.Deserialize<PaystackWebhookEvent>(json);

                    if (webhookEvent?.Event == "charge.success")
                    {
                        var data = webhookEvent.Data;

                        // Create or update tenant
                        var tenant = new TenantConfig
                        {
                            Id = data.Reference,
                            Name = $"{data.Customer?.FirstName} {data.Customer?.LastName}",
                            Email = data.Customer?.Email!,
                            PlanCode = data.Plan?.PlanCode,
                            Plan = data.Plan?.Name!,
                            Amount = data.Amount / 100,
                            SubscribedAt = DateTime.UtcNow,
                            IsActive = true,
                            PaymentReference = data.Reference,

                            // AI defaults
                            SystemPrompt = "You are a helpful AI assistant for this business.",
                            Model = "bizbot-chat",
                            MaxTokens = 800
                        };

                        //await cosmosService.CreateTenantAsync(tenant);
                        await cosmosService.UpsertTenantAsync(tenant);
                    }
                    else if (webhookEvent?.Event == "subscription.not_renew")
                    {
                        // Handle subscription cancellation
                        // Update tenant status in Cosmos DB
                    }

                    return Results.Ok();
                }
                catch (Exception)
                {
                    return Results.BadRequest();
                }
            })
            .WithName("PaystackWebhook");

            // Get available plans
            group.MapGet("/plans", () =>
            {
                var plans = new[]
                {
                    new
                    {
                        id = "starter",
                        name = "BizBot Starter",
                        description = "1,000 messages/month, basic features",
                        price = 29.00m,
                        currency = "USD",
                        interval = "monthly",
                        features = new[] { "1,000 messages/month", "Basic customization", "Email support" }
                    },
                    new
                    {
                        id = "pro",
                        name = "BizBot Pro",
                        description = "10,000 messages/month, advanced features",
                        price = 99.00m,
                        currency = "USD",
                        interval = "monthly",
                        features = new[] { "10,000 messages/month", "Custom branding", "Priority support", "Advanced AI" }
                    },
                    new
                    {
                        id = "agency",
                        name = "BizBot Agency",
                        description = "Unlimited clients, white-label solution",
                        price = 299.00m,
                        currency = "USD",
                        interval = "monthly",
                        features = new[] { "Unlimited clients", "White-label solution", "API access", "Dedicated support" }
                    }
                };

                return Results.Ok(plans);
            })
            .WithName("GetPlans");
        }
    }
}