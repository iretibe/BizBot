using BizBot.Client.Enums;

namespace BizBot.Client.Responses
{
    public record ChatResponse(string Message, 
        string ConversationId, ConversationSource Source);
}
