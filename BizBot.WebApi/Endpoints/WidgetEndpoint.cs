using BizBot.WebApi.Models;
using BizBot.WebApi.Services;

namespace BizBot.WebApi.Endpoints
{
    public static class WidgetEndpoints
    {
        public static void MapWidgetEndpoints(this WebApplication app)
        {
            app.MapGet("/api/widget/config", async (string tenant,
                CosmosDbService cosmos) =>
            {
                var tenantConfig = await cosmos.GetTenantByIdAsync(tenant);

                if (tenantConfig == null || !tenantConfig.IsActive)
                    return Results.NotFound();

                var canWhiteLabel = PlanLimits.CanUseWhiteLabel(tenantConfig.Plan!);

                return Results.Ok(new
                {
                    welcomeMessage =
                        tenantConfig.WhiteLabelSettings?.WelcomeMessage
                        ?? "Hi there, How can I help you today?",

                    theme = tenantConfig.WhiteLabelSettings,

                    showBranding = !canWhiteLabel,

                    canWhiteLabel
                });
            });
        }
    }
}
