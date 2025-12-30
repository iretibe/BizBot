using BizBot.WebApi.Endpoints;
using BizBot.WebApi.Helpers;
using BizBot.WebApi.Interfaces;
using BizBot.WebApi.Services;
using Microsoft.Azure.Cosmos;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(
        int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "8080"));
});

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS – SAFE widget policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("WidgetPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://widget.bizbot.space",
                "https://app.bizbot.space"
            // add client domains here later if needed
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Rate Limiting (chat protection)
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("chat-per-tenant", context =>
    {
        var tenantId =
            context.User.FindFirst("tid")?.Value
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            tenantId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 5,
                QueueProcessingOrder =
                    QueueProcessingOrder.OldestFirst
            });
    });
});

// Logging
builder.Services.AddLogging();

// Azure Cosmos DB
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

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<PaystackService>();
builder.Services.AddScoped<AzureSearchIndexManager>();
builder.Services.AddScoped<AzureAISearchService>();
builder.Services.AddScoped<WidgetTokenService>();

var app = builder.Build();

// Initialize Cosmos DB containers ONCE
var cosmos = app.Services.GetRequiredService<CosmosDbService>();
await cosmos.InitializeAsync();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Create search index (dev only)
    try
    {
        using var scope = app.Services.CreateScope();
        var indexManager = scope.ServiceProvider
            .GetRequiredService<AzureSearchIndexManager>();

        await indexManager.CreateSimpleIndexAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"Warning: Could not create search index: {ex.Message}");
    }
}

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// CORS must come BEFORE endpoints
app.UseCors("WidgetPolicy");

// Rate limiting middleware
app.UseRateLimiter();

// CSP (iframe embedding control)
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] =
        "frame-ancestors https://widget.bizbot.space https://app.bizbot.space";
    await next();
});

// Endpoints
app.MapChatEndpoints();          // apply limiter inside this group
app.MapSubscriptionEndpoints();
app.MapSearchEndpoints();
app.MapSettingsEndpoints();
app.MapWidgetEndpoints();
app.MapAdminEndpoints();
app.MapPaystackEndpoints();

app.MapGet("/", () => "BizBot API is running!");
app.MapGet("/health", () =>
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .ExcludeFromDescription(); ;

app.MapGet("/error", () =>
    Results.Problem("An unexpected error occurred."));

app.Run();
