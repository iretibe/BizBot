namespace BizBot.WebApi.Models
{
    public record IndexDocumentRequest(string TenantId,
        string DocumentId, string Content, string? Title = null);
}
