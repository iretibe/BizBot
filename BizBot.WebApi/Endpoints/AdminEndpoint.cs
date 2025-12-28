using BizBot.WebApi.Models;
using BizBot.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BizBot.WebApi.Endpoints
{
    public static class AdminEndpoint
    {
        public static void MapAdminEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/admin");

            group.MapPost("/admin/tenants/{id}/plan", async (string id,
                [FromBody] ChangePlanRequest request, CosmosDbService cosmos) =>
            {
                var tenant = await cosmos.GetTenantByIdAsync(id);
                if (tenant == null) return Results.NotFound();

                tenant.Plan = request.NewPlan;

                if (!PlanLimits.CanUseWhiteLabel(request.NewPlan))
                {
                    tenant.WhiteLabelSettings = null;
                }

                await cosmos.UpsertTenantAsync(tenant);
                return Results.Ok();
            });

            group.MapGet("/admin/tenants", async (CosmosDbService cosmos) =>
            {
                var tenants = await cosmos.GetAllTenantsAsync();

                return tenants.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Plan,
                    t.IsActive,
                    CreatedAt = t.SubscribedAt
                });
            });


            group.MapGet("/admin/usage/daily", async (CosmosDbService cosmos) =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var tenants = await cosmos.GetAllTenantsAsync();

                var results = new List<object>();

                foreach (var tenant in tenants)
                {
                    var usage = await cosmos.GetDailyUsageAsync(tenant.Id, today);

                    results.Add(new
                    {
                        TenantId = tenant.Id,
                        MessagesToday = usage.Messages,
                        TokensToday = 0 // add token tracking later
                    });
                }

                return results;
            });


            group.MapGet("/admin/conversations/count", async (CosmosDbService cosmos) =>
            {
                var count = await cosmos.GetConversationCountAsync();
                return Results.Ok(new { TotalConversations = count });
            });
        }
    }
}
