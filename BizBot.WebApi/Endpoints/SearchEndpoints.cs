using BizBot.WebApi.Models;
using BizBot.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BizBot.WebApi.Endpoints
{
    public static class SearchEndpoints
    {
        public static void MapSearchEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/search");

            group.MapPost("/index", async (
                [FromBody] IndexDocumentRequest request,
                [FromServices] AzureAISearchService searchService) =>
            {
                await searchService.IndexDocumentAsync(
                    request.TenantId, request.DocumentId,
                    request.Content, request.Title);

                return Results.Ok(new { success = true });
            }).WithName("IndexDocument");

            group.MapGet("/search/{tenantId}", async (
                string tenantId,
                [FromQuery] string query,
                [FromServices] AzureAISearchService searchService) =>
            {
                var context = await searchService.SearchRelevantContextSimpleAsync(query, tenantId);
                
                return Results.Ok(new { context });
            }).WithName("Search");

            group.MapDelete("/document/{documentId}", async (
                string documentId,
                [FromServices] AzureAISearchService searchService) =>
            {
                await searchService.DeleteDocumentAsync(documentId);
                
                return Results.Ok(new { success = true });
            }).WithName("DeleteDocument");
        }
    }
}