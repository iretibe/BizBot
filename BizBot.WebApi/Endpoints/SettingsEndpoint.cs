using BizBot.WebApi.Models;
using BizBot.WebApi.Services;
using System.Security.Claims;

namespace BizBot.WebApi.Endpoints
{
    public static class SettingsEndpoints
    {
        public static void MapSettingsEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/settings").RequireAuthorization();

            // GET white-label settings
            group.MapGet("/white-label", async (CosmosDbService cosmos,
                ClaimsPrincipal user) =>
            {
                var tenant = await cosmos.GetTenantFromUserAsync(user);

                if (!PlanLimits.CanUseWhiteLabel(tenant.Plan!))
                    return Results.Forbid();

                return Results.Ok(tenant.WhiteLabelSettings);
            });

            // UPDATE white-label settings
            group.MapPost("/white-label", async (WhiteLabelSettings settings,
                CosmosDbService cosmos, ClaimsPrincipal user) =>
            {
                var tenant = await cosmos.GetTenantFromUserAsync(user);

                if (!PlanLimits.CanUseWhiteLabel(tenant.Plan!))
                    return Results.Forbid();

                tenant.WhiteLabelSettings = settings;
                await cosmos.UpsertTenantAsync(tenant);

                return Results.Ok();
            });
        }
    }
}
