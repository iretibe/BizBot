using BizBot.WebApi.Endpoints;
using BizBot.WebApi.Helpers;
using BizBot.WebApi.Interfaces;
using BizBot.WebApi.Services;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWidget", builder =>
        builder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true)
    );
});

// Add logging
builder.Services.AddLogging();

// Add Azure Cosmos DB client
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    return new CosmosClient(
        config["CosmosDb:ConnectionString"],
        new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                IgnoreNullValues = true
            }
        });
});
builder.Services.AddSingleton<CosmosDbService>();

builder.Services.AddHttpClient();

builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>();

// Register CosmosDbService as singleton so InitializeAsync configures the single instance used by all requests
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<PaystackService>();
builder.Services.AddScoped<AzureSearchIndexManager>();
builder.Services.AddScoped<AzureAISearchService>();

var app = builder.Build();

// Initialize Cosmos DB containers ONCE at startup
var cosmos = app.Services.GetRequiredService<CosmosDbService>();
await cosmos.InitializeAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Initialize search index in development
    try
    {
        using var scope = app.Services.CreateScope();
        var indexManager = scope.ServiceProvider.GetRequiredService<AzureSearchIndexManager>();
        await indexManager.CreateSimpleIndexAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not create search index: {ex.Message}");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowWidget");

// Map endpoints
app.MapChatEndpoints();
app.MapSubscriptionEndpoints();
app.MapSearchEndpoints();

app.MapGet("/", () => "BizBot API is running!");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
