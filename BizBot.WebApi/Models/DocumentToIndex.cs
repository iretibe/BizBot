namespace BizBot.WebApi.Models
{
    public class DocumentToIndex
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // e.g., "website", "pdf", "manual"
    }
}
