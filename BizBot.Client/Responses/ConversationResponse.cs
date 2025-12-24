namespace BizBot.Client.Responses
{
    public record ConversationResponse(string UserMessage, string AssistantMessage, DateTime Timestamp);
}
