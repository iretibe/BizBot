using BizBot.WebApi.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net;
using System.Security.Claims;
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
                        PartitionKeyPath = "/id",
                        IndexingPolicy = new IndexingPolicy
                        {
                            IncludedPaths =
                            {
                                new IncludedPath { Path = "/id/?" },
                                new IncludedPath { Path = "/email/?" },
                                new IncludedPath { Path = "/plan/?" },
                                new IncludedPath { Path = "/isActive/?" }
                            }
                        }
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
        public async Task LogConversationAsync(string tenantId,
            string conversationId, string userMessage, string assistantMessage)
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

        public async Task<TenantConfig?> GetTenantByEmailAsync(string email)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                .WithParameter("@email", email);

            using var iterator = _tenantsContainer!.GetItemQueryIterator<TenantConfig>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return null;
        }

        public async Task<TenantConfig> GetTenantFromUserAsync(ClaimsPrincipal user)
        {
            var email = user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedAccessException("Authenticated user has no email");

            var tenant = await GetTenantByEmailAsync(email);

            if (tenant == null)
                throw new InvalidOperationException("Tenant not found");

            return tenant;
        }

        public async Task<TenantConfig?> GetTenantByIdAsync(string tenantId)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", tenantId);

            using var iterator = _tenantsContainer!.GetItemQueryIterator<TenantConfig>(query);

            if (!iterator.HasMoreResults)
                return null;

            var response = await iterator.ReadNextAsync();
            return response.Resource.FirstOrDefault();
        }

        public async Task<TenantDailyUsage> GetDailyUsageAsync(string tenantId, DateOnly date)
        {
            EnsureInitialized();

            var id = $"{tenantId}_{date:yyyy-MM-dd}";

            try
            {
                var response = await _tenantsContainer!.ReadItemAsync<TenantDailyUsage>(
                    id,
                    new PartitionKey(tenantId));

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return new TenantDailyUsage
                {
                    Id = id,
                    TenantId = tenantId,
                    Date = date.ToString("yyyy-MM-dd"),
                    Messages = 0
                };
            }
        }

        public async Task IncrementUsageAsync(string tenantId, DateOnly date)
        {
            EnsureInitialized();

            var id = $"{tenantId}_{date:yyyy-MM-dd}";

            try
            {
                var response = await _tenantsContainer!.ReadItemAsync<TenantDailyUsage>(
                    id,
                    new PartitionKey(tenantId));

                response.Resource.Messages++;

                await _tenantsContainer.UpsertItemAsync(
                    response.Resource,
                    new PartitionKey(tenantId));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var usage = new TenantDailyUsage
                {
                    Id = id,
                    TenantId = tenantId,
                    Date = date.ToString("yyyy-MM-dd"),
                    Messages = 1
                };

                await _tenantsContainer!.UpsertItemAsync(
                    usage,
                    new PartitionKey(tenantId));
            }
        }

        public async Task<int> CountConversationsAsync(string tenantId)
        {
            EnsureInitialized();

            var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.tenantId = @tenantId")
                .WithParameter("@tenantId", tenantId);

            using var iterator = _conversationsContainer!
                .GetItemQueryIterator<int>(
                    query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(tenantId)
                    });

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return 0;
        }

        public async Task<IEnumerable<TenantConfig>> GetAllTenantsAsync()
        {
            var query = _tenantsContainer!
                .GetItemLinqQueryable<TenantConfig>(allowSynchronousQueryExecution: true)
                .Where(t => true)
                .ToFeedIterator();

            var results = new List<TenantConfig>();
            while (query.HasMoreResults)
                results.AddRange(await query.ReadNextAsync());

            return results;
        }

        public async Task<int> GetConversationCountAsync()
        {
            var query = _conversationsContainer!
                .GetItemLinqQueryable<Conversation>(allowSynchronousQueryExecution: true)
                .Select(c => c.Id)
                .ToFeedIterator();

            int count = 0;
            while (query.HasMoreResults)
                count += (await query.ReadNextAsync()).Count;

            return count;
        }

        public async Task UpsertDailyUsageAsync(TenantDailyUsage usage)
        {
            await _tenantsContainer!.UpsertItemAsync(
                usage,
                new PartitionKey(usage.TenantId));
        }

        public async Task<bool> HasProcessedWebhookAsync(string eventType, string reference)
        {
            EnsureInitialized();

            var id = $"{eventType}_{reference}";

            try
            {
                await _tenantsContainer!.ReadItemAsync<ProcessedWebhook>(
                    id,
                    new PartitionKey(id));

                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task MarkWebhookProcessedAsync(string eventType, string reference)
        {
            EnsureInitialized();

            var item = new ProcessedWebhook
            {
                Id = $"{eventType}_{reference}",
                EventType = eventType,
                Reference = reference
            };

            await _tenantsContainer!.UpsertItemAsync(
                item,
                new PartitionKey(item.Id));
        }

        public async Task MarkSubscriptionPaidAsync(string reference)
        {
            EnsureInitialized();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.paymentReference = @ref")
                .WithParameter("@ref", reference);

            using var iterator =
                _tenantsContainer!.GetItemQueryIterator<TenantConfig>(query);

            if (!iterator.HasMoreResults) return;

            var response = await iterator.ReadNextAsync();
            var tenant = response.Resource.FirstOrDefault();
            if (tenant == null) return;

            tenant.IsActive = true;
            tenant.SubscribedAt = DateTime.UtcNow;

            await UpsertTenantAsync(tenant);
        }
    }
}
