using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using BizBot.WebApi.Models;

namespace BizBot.WebApi.Helpers
{
    public class AzureSearchIndexManager
    {
        private readonly SearchIndexClient _adminClient;
        private readonly IConfiguration _configuration;

        public AzureSearchIndexManager(IConfiguration configuration)
        {
            _configuration = configuration;

            var serviceEndpoint = configuration["AzureSearch:Endpoint"];
            var apiKey = configuration["AzureSearch:ApiKey"];

            _adminClient = new SearchIndexClient(
                new Uri(serviceEndpoint!),
                new AzureKeyCredential(apiKey!));
        }

        public async Task CreateOrUpdateIndexAsync()
        {
            var indexName = _configuration["AzureSearch:IndexName"];

            // Build fields using FieldBuilder
            var fields = new FieldBuilder().Build(typeof(KnowledgeDocument));

            var indexDefinition = new SearchIndex(indexName!)
            {
                Fields = fields
            };

            // Use the direct, public methods instead of reflection
            AddSemanticSearchConfiguration(indexDefinition);

            AddVectorSearchSettings(indexDefinition);

            await _adminClient.CreateOrUpdateIndexAsync(indexDefinition);
            Console.WriteLine($"Index '{indexName}' created/updated successfully.");
        }

        // Renamed and updated to use the correct types
        private void AddSemanticSearchConfiguration(SearchIndex index)
        {
            var semanticConfigName = _configuration["AzureSearch:SemanticConfigurationName"];
            if (!string.IsNullOrEmpty(semanticConfigName))
            {
                // The modern way to define semantic search is via the SemanticSearch property
                index.SemanticSearch = new SemanticSearch
                {
                    Configurations =
                    {
                        new SemanticConfiguration(semanticConfigName, new()
                        {
                            TitleField = new SemanticField("title"),
                            ContentFields =
                            {
                                new SemanticField("content")
                            }
                        })
                    }
                };
                Console.WriteLine("Semantic search settings added to index.");
            }
        }

        private void AddVectorSearchSettings(SearchIndex index)
        {
            // The VectorSearch property is public, no reflection needed.
            // Create simple vector search configuration
            index.VectorSearch = new VectorSearch
            {
                Profiles =
                {
                    new VectorSearchProfile("my-vector-profile", "my-vector-algorithm")
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("my-vector-algorithm")
                }
            };
            Console.WriteLine("Vector search settings added to index.");
        }

        // Simple index creation without advanced features
        public async Task CreateSimpleIndexAsync()
        {
            var indexName = _configuration["AzureSearch:IndexName"];

            // Use simple field definitions
            var fields = new List<SearchField>
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("tenantId") { IsFilterable = true },
                new SearchableField("title") { IsFilterable = true },
                new SearchableField("content") { AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft },
                new SimpleField("timestamp", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SearchableField("source") { IsFilterable = true }
            };

            var index = new SearchIndex(indexName!)
            {
                Fields = fields
            };

            await _adminClient.CreateOrUpdateIndexAsync(index);
            Console.WriteLine($"Simple index '{indexName}' created successfully.");
        }

        public async Task<bool> IndexExistsAsync(string indexName)
        {
            try
            {
                await _adminClient.GetIndexAsync(indexName);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        public async Task DeleteIndexAsync(string indexName)
        {
            await _adminClient.DeleteIndexAsync(indexName);
            Console.WriteLine($"Index '{indexName}' deleted.");
        }

        public async Task<List<string>> ListIndexesAsync()
        {
            var indexes = new List<string>();
            await foreach (var index in _adminClient.GetIndexesAsync())
            {
                indexes.Add(index.Name);
            }

            return indexes;
        }
    }
}
