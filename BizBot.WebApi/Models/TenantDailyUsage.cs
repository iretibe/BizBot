using System.Text.Json.Serialization;

namespace BizBot.WebApi.Models
{
    public class TenantDailyUsage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!; // tenantId_yyyy-MM-dd
        
        public string TenantId { get; set; } = default!;
        public string Date { get; set; } = default!;
        public int Messages { get; set; }
    }
}
