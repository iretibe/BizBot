using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace BizBot.WebApi.Services
{
    public class AzureAISearchService
    {
        private readonly SearchClient _searchClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureAISearchService> _logger;

        public AzureAISearchService(IConfiguration configuration, ILogger<AzureAISearchService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var serviceEndpoint = configuration["AzureSearch:Endpoint"];
            var indexName = configuration["AzureSearch:IndexName"];
            var apiKey = configuration["AzureSearch:ApiKey"];

            _searchClient = new SearchClient(
                new Uri(serviceEndpoint!),
                indexName!,
                new AzureKeyCredential(apiKey!));
        }

        public async Task<string> SearchRelevantContextSimpleAsync(string query, string tenantId)
        {
            try
            {
                var searchOptions = new SearchOptions
                {
                    Filter = $"tenantId eq '{tenantId}'",
                    Size = 3,
                    QueryType = SearchQueryType.Simple
                };

                var results = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);

                var contextBuilder = new System.Text.StringBuilder();
                await foreach (var result in results.Value.GetResultsAsync())
                {
                    if (result.Document.TryGetValue("content", out var content))
                    {
                        contextBuilder.AppendLine($"• {content}");
                    }
                }

                return contextBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed for query: {Query}", query);
                return string.Empty;
            }
        }

        public async Task IndexDocumentAsync(string tenantId, string documentId, string content, string? title = null)
        {
            try
            {
                var document = new SearchDocument
                {
                    ["id"] = documentId,
                    ["tenantId"] = tenantId,
                    ["content"] = content,
                    ["title"] = title ?? "Untitled",
                    ["timestamp"] = DateTime.UtcNow
                };

                var batch = IndexDocumentsBatch.Upload(new[] { document });
                await _searchClient.IndexDocumentsAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index document: {DocumentId}", documentId);
                throw;
            }
        }

        public async Task DeleteDocumentAsync(string documentId)
        {
            try
            {
                var document = new SearchDocument { ["id"] = documentId };
                var batch = IndexDocumentsBatch.Delete(new[] { document });
                await _searchClient.IndexDocumentsAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document: {DocumentId}", documentId);
                throw;
            }
        }
    }
}