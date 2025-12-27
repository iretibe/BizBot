using BizBot.WebApi.Events;
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
                [FromServices] PaystackService paystackService,
                IConfiguration config, CosmosDbService cosmos) =>
            {
                try
                {
                    // Generate reference
                    var reference = $"BIZBOT_{Guid.NewGuid():N}";

                    // Initialize Paystack
                    var response = await paystackService.CreateSubscriptionAsync(
                        request.PlanName,
                        request.CustomerEmail,
                        reference,
                        new Dictionary<string, string>
                        {
                            ["plan"] = request.PlanName,
                            ["billing_cycle"] = request.BillingCycle
                        }
                    );

                    return Results.Ok(new
                    {
                        authorizationUrl = response.Data.AuthorizationUrl,
                        reference,
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
            group.MapGet("/verify/{reference}", async (string reference, 
                PaystackService paystack, CosmosDbService cosmos, IConfiguration config) =>
            {
                var verification = await paystack.VerifyTransactionAsync(reference);

                if (verification.Data.Status != "success")
                    return Results.BadRequest("Payment not successful");

                // Find tenant (or create)
                var tenant = await cosmos.GetTenantByEmailAsync(
                    verification.Data.Customer.Email)
                    ?? new TenantConfig
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = verification.Data.Customer.Email
                    };

                // Load plans
                var plans = config
                    .GetSection("SubscriptionPlans")
                    .Get<List<SubscriptionPlan>>()!;

                var plan = plans.Single(p => p.Key == verification.Data.Metadata.Plan);

                var planCode = verification.Data.Metadata.BillingCycle == "yearly"
                    ? plan.YearlyPlanCode
                    : plan.MonthlyPlanCode;

                // Persist billing info
                tenant.Plan = plan.Key;
                tenant.BillingCycle = verification.Data.Metadata.BillingCycle;
                tenant.PlanCode = planCode;
                tenant.IsActive = true;
                tenant.SubscribedAt = DateTime.UtcNow;

                await cosmos.UpsertTenantAsync(tenant);

                return Results.Ok();
            })
            .WithName("VerifyTransaction");


            //// Verify transaction (callback from PayStack)
            //group.MapGet("/verify/{reference}", async (
            //    string reference,
            //    [FromServices] PaystackService paystackService,
            //    [FromServices] CosmosDbService cosmosService) =>
            //{
            //    try
            //    {
            //        var verification = await paystackService.VerifyTransactionAsync(reference);

            //        if (verification.Status && verification.Data.Status == "success")
            //        {
            //            // Create tenant in Cosmos DB
            //            var tenant = new TenantConfig
            //            {
            //                Id = reference,
            //                Name = $"{verification.Data.Customer.FirstName} {verification.Data.Customer.LastName}",
            //                Email = verification.Data.Customer.Email,
            //                Plan = verification.Data.Plan.Name,
            //                PlanCode = verification.Data.Plan.PlanCode,
            //                Amount = verification.Data.Amount / 100, // Convert back from kobo
            //                SubscribedAt = DateTime.UtcNow,
            //                IsActive = true,
            //                PaymentReference = reference,

            //                // AI defaults
            //                SystemPrompt = "You are a helpful AI assistant for this business.",
            //                Model = "bizbot-chat",
            //                MaxTokens = 800
            //            };

            //            //await cosmosService.CreateTenantAsync(tenant);
            //            await cosmosService.UpsertTenantAsync(tenant);

            //            // Redirect to success page
            //            return Results.Redirect("https://your-app.com/success");
            //        }
            //        else
            //        {
            //            // Redirect to failure page
            //            return Results.Redirect($"https://your-app.com/failed?error={verification.Message}");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        return Results.Problem($"Verification error: {ex.Message}");
            //    }
            //})
            //.WithName("VerifyTransaction");

            // PayStack webhook endpoint
            group.MapPost("/webhook", async (HttpRequest request,
                PaystackService paystack, CosmosDbService cosmos,
                IConfiguration config) =>
            {
                var json = await new StreamReader(request.Body).ReadToEndAsync();

                var signature = request.Headers["x-paystack-signature"].FirstOrDefault();
                if (!paystack.ValidateWebhookSignature(json, signature!))
                    return Results.Unauthorized();

                var webhook = JsonSerializer.Deserialize<PaystackWebhookEvent>(json);
                if (webhook is null)
                    return Results.BadRequest();

                switch (webhook.Event)
                {
                    case "charge.success":
                        await HandleChargeSuccess(webhook, cosmos, config);
                        break;

                    case "subscription.disable":
                        await HandleSubscriptionDisabled(webhook, cosmos);
                        break;
                }

                return Results.Ok();
            });

            //group.MapPost("/webhook", async (HttpRequest request,
            //    [FromServices] PaystackService paystackService,
            //    [FromServices] CosmosDbService cosmosService) =>
            //{
            //    // Read the request body
            //    var json = await new StreamReader(request.Body).ReadToEndAsync();

            //    try
            //    {
            //        // Validate webhook signature
            //        var signature = request.Headers["x-paystack-signature"].FirstOrDefault();
            //        if (!paystackService.ValidateWebhookSignature(json, signature!))
            //            return Results.Unauthorized();

            //        // Parse webhook event
            //        var webhookEvent = JsonSerializer.Deserialize<PaystackWebhookEvent>(json);

            //        if (webhookEvent?.Event == "charge.success")
            //        {
            //            var data = webhookEvent.Data;

            //            // Create or update tenant
            //            var tenant = new TenantConfig
            //            {
            //                Id = data.Reference,
            //                Name = $"{data.Customer?.FirstName} {data.Customer?.LastName}",
            //                Email = data.Customer?.Email!,
            //                PlanCode = data.Plan?.PlanCode,
            //                Plan = data.Plan?.Name!,
            //                Amount = data.Amount / 100,
            //                SubscribedAt = DateTime.UtcNow,
            //                IsActive = true,
            //                PaymentReference = data.Reference,

            //                // AI defaults
            //                SystemPrompt = "You are a helpful AI assistant for this business.",
            //                Model = "bizbot-chat",
            //                MaxTokens = 800
            //            };

            //            //await cosmosService.CreateTenantAsync(tenant);
            //            await cosmosService.UpsertTenantAsync(tenant);
            //        }
            //        else if (webhookEvent?.Event == "subscription.not_renew")
            //        {
            //            // Handle subscription cancellation
            //            // Update tenant status in Cosmos DB
            //        }

            //        return Results.Ok();
            //    }
            //    catch (Exception)
            //    {
            //        return Results.BadRequest();
            //    }
            //})
            //.WithName("PaystackWebhook");

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


            //group.MapPost("/change-plan", async (ChangePlanRequest request,
            //    CosmosDbService cosmos, PaystackService paystack) =>
            //{
            //    var tenant = await cosmos.GetTenantAsync(request.TenantId);

            //    // 1. Disable old subscription
            //    await paystack.DisableSubscriptionAsync(
            //        tenant.SubscriptionCode,
            //        tenant.EmailToken);

            //    // 2. Start new subscription
            //    var reference = $"BIZBOT_{Guid.NewGuid():N}";

            //    var response = await paystack.CreateSubscriptionAsync(
            //        request.NewPlanCode,
            //        tenant.Email,
            //        reference,
            //        metadata);

            //    return Results.Ok(response);
            //});
        }

        private static async Task HandleSubscriptionDisabled(
            PaystackWebhookEvent webhook, CosmosDbService cosmos)
        {
            var data = webhook.Data;

            var tenant = await cosmos.GetTenantByEmailAsync(data.Customer!.Email);
            if (tenant == null)
                return;

            tenant.IsActive = false;
            tenant.CancelledAt = DateTime.UtcNow;

            await cosmos.UpsertTenantAsync(tenant);
        }

        private static async Task HandleChargeSuccess(
            PaystackWebhookEvent webhook, CosmosDbService cosmos, 
            IConfiguration config)
        {
            var data = webhook.Data;

            // Always identify tenant by email (or stored tenant id)
            var tenant = await cosmos.GetTenantByEmailAsync(data.Customer!.Email)
                ?? new TenantConfig
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = data.Customer.Email
                };

            // Extract metadata (SOURCE OF TRUTH)
            var planKey = data.Metadata.Plan;
            var billingCycle = data.Metadata.BillingCycle;

            // Load plan config
            var plans = config
                .GetSection("SubscriptionPlans")
                .Get<List<SubscriptionPlan>>()!;

            var plan = plans.Single(p => p.Key == planKey);

            var planCode = billingCycle == "yearly"
                ? plan.YearlyPlanCode
                : plan.MonthlyPlanCode;

            // Persist billing state
            tenant.Plan = planKey;
            tenant.BillingCycle = billingCycle;
            tenant.PlanCode = planCode;
            tenant.SubscriptionCode = data.Subscription?.SubscriptionCode;
            tenant.EmailToken = data.Subscription?.EmailToken;
            tenant.IsActive = true;
            tenant.SubscribedAt = DateTime.UtcNow;
            tenant.PaymentReference = data.Reference;

            await cosmos.UpsertTenantAsync(tenant);
        }
    }
}