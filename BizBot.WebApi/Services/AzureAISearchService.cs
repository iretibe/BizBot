using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using BizBot.WebApi.Responses;

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

        public async Task<List<KnowledgeChunk>> SearchRelevantChunksAsync(string query, string tenantId)
        {
            var chunks = new List<KnowledgeChunk>();

            var searchOptions = new SearchOptions
            {
                Filter = $"tenantId eq '{tenantId}'",
                Size = _configuration.GetValue<int>("AzureSearch:MaxResults"),
                QueryType = SearchQueryType.Semantic,
                SearchMode = SearchMode.All // Ensure it doesn't just match common words
            };

            searchOptions.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = _configuration["AzureSearch:SemanticConfigurationName"]
            };

            searchOptions.SearchFields.Add("content");
            searchOptions.SearchFields.Add("title");

            var results = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);
            var minScore = _configuration.GetValue<double>("AzureSearch:MinRelevanceScore");

            await foreach (var result in results.Value.GetResultsAsync())
            {
                // The RerankerScore is set to (0-4 scale) if available
                double? finalScore = result.SemanticSearch?.RerankerScore ?? result.Score;

                if (finalScore < minScore)
                    continue;

                var content = result.Document.TryGetValue("content", out var c) ? c?.ToString() : null;
                var title = result.Document.TryGetValue("title", out var t) ? t?.ToString() : null;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    chunks.Add(new KnowledgeChunk(
                        Content: content,
                        Title: title,
                        Score: finalScore
                    ));
                }
            }

            return chunks.OrderByDescending(c => c.Score).ToList();
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