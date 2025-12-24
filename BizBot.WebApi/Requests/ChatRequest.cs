using BizBot.WebApi.Enums;

namespace BizBot.WebApi.Requests
{
    public record ChatRequest(string Message,
        ConversationSource Source, string? ConversationId = null);
}
