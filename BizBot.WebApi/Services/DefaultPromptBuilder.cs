namespace BizBot.WebApi.Services
{
    public static class DefaultPromptBuilder
    {
        public static string Build(string knowledgeContext) => $"""
            You are BizBot, a business AI assistant.

            Rules:
            - Answer ONLY using the knowledge below.
            - If not found, say:
              "I don't have that information yet."

            --- KNOWLEDGE START ---
            {knowledgeContext}
            --- KNOWLEDGE END ---
            """
        ;
    }
}
