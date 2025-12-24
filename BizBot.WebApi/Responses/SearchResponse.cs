namespace BizBot.WebApi.Responses
{
    public record SearchResultItem(string Content, string? Title);

    public record SearchResponse(List<SearchResultItem> Results);
}
