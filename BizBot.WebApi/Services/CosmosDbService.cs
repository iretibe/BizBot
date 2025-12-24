using BizBot.WebApi.Models;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Text.Json;

namespace BizBot.WebApi.Services
{
    public sealed class CosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _configuration;

        private Container? _tenantsContainer;
        private Container? _conversationsContainer;

        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        public CosmosDbService(CosmosClient cosmosClient, IConfiguration configuration)
        {
            _cosmosClient = cosmosClient;
            _configuration = configuration;
        }

        // Initialization (Call once at startup)
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;

                var databaseId = _configuration["CosmosDb:DatabaseId"] ?? "BizBotDb";
                var tenantsContainerId = _configuration["CosmosDb:TenantsContainerId"] ?? "Tenants";
                var conversationsContainerId = _configuration["CosmosDb:ConversationsContainerId"] ?? "Conversations";

                var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

                _tenantsContainer = await database.Database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties
                    {
                        Id = tenantsContainerId,
                        PartitionKeyPath = "/id"
                    });

                _conversationsContainer = await database.Database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties
                    {
                        Id = conversationsContainerId,
                        PartitionKeyPath = "/tenantId"
                    });

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private void EnsureInitialized()
        {
            if (!_initialized || _tenantsContainer is null || _conversationsContainer is null)
            {
                throw new InvalidOperationException(
                    "CosmosDbService is not initialized. Call InitializeAsync() once at startup.");
            }
        }

        // Tenant: Read or Create (No Exceptions for 404)
        public async Task<TenantConfig> GetTenantConfigAsync(string tenantId)
        {
            EnsureInitialized();

            var response = await _tenantsContainer!.ReadItemStreamAsync(
                tenantId,
                new PartitionKey(tenantId));

            if (response.IsSuccessStatusCode)
            {
                var tenant = await JsonSerializer.DeserializeAsync<TenantConfig>(
                    response.Content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return tenant
                    ?? throw new InvalidOperationException("Failed to deserialize tenant config.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var defaultTenant = CreateDefaultTenant(tenantId);
                await UpsertTenantAsync(defaultTenant);
                return defaultTenant;
            }

            throw new InvalidOperationException(
                $"Unexpected Cosmos DB response: {response.StatusCode}");
        }

        private static TenantConfig CreateDefaultTenant(string tenantId)
        {
            return new TenantConfig
            {
                Id = tenantId,
                Name = "Default Tenant",
                IsActive = true,

                // AI defaults
                SystemPrompt = "You are a helpful AI assistant for a business website.",
                Model = "gpt-4o-mini",
                //Model = "bizbot-chat",
                MaxTokens = 1000,

                SubscribedAt = DateTime.UtcNow
            };
        }

        // Tenant Upsert (Idempotent)
        public async Task UpsertTenantAsync(TenantConfig tenant)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(tenant.Id))
                throw new ArgumentException("Tenant Id is required");

            if (tenant.Id.Trim().Length == 0)
                throw new ArgumentException("Tenant Id cannot be empty");

            try
            {
                var response = await _tenantsContainer!.UpsertItemAsync(
                    tenant,
                    new PartitionKey(tenant.Id));

                // Optional: log RU charge
                Console.WriteLine($"Upsert succeeded. RU: {response.RequestCharge}");
            }
            catch (CosmosException ex)
            {
                Console.WriteLine("Cosmos Upsert FAILED");
                Console.WriteLine($"StatusCode: {ex.StatusCode}");
                Console.WriteLine($"SubStatus: {ex.SubStatusCode}");
                Console.WriteLine($"ActivityId: {ex.ActivityId}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Diagnostics: {ex.Diagnostics}");

                // Log serialized payload (VERY IMPORTANT)
                Console.WriteLine("Serialized tenant:");
                Console.WriteLine(JsonSerializer.Serialize(tenant, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

                throw;
            }
        }

        // Conversation Logging
        public async Task LogConversationAsync(
            string tenantId,
            string conversationId,
            string userMessage,
            string assistantMessage)
        {
            EnsureInitialized();

            var conversation = new Conversation
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                ConversationId = conversationId,
                UserMessage = userMessage,
                AssistantMessage = assistantMessage,
                Timestamp = DateTime.UtcNow
            };

            await _conversationsContainer!.CreateItemAsync(
                conversation,
                new PartitionKey(tenantId));
        }

        // Conversation History Retrieval
        public async Task<List<Conversation>> GetConversationHistoryAsync(
            string tenantId, string conversationId)
        {
            EnsureInitialized();

            var query = new QueryDefinition(
                @"SELECT * FROM c
                  WHERE c.tenantId = @tenantId
                  AND c.conversationId = @conversationId
                  ORDER BY c.timestamp")
                .WithParameter("@tenantId", tenantId)
                .WithParameter("@conversationId", conversationId);

            var iterator = _conversationsContainer!
                .GetItemQueryIterator<Conversation>(query);

            var results = new List<Conversation>();

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                results.AddRange(page);
            }

            return results;
        }
    }
}
