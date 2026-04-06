using BizBot.WebApi.Endpoints;
using BizBot.WebApi.Helpers;
using BizBot.WebApi.Interfaces;
using BizBot.WebApi.Services;
using Microsoft.Azure.Cosmos;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

#region Kestrel / Linux App Service
// Required for Azure Linux App Service
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(
        int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "8080"));
});
#endregion

#region Core services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endregion

#region CORS – Widget safe policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("WidgetPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://widget.bizbot.space",
                "https://app.bizbot.space"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
#endregion

#region Rate limiting (per tenant)
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("chat-per-tenant", context =>
    {
        var tenantId =
            context.User.FindFirst("tid")?.Value
            ?? context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            tenantId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});
#endregion

#region Logging
builder.Services.AddLogging();
#endregion

#region Azure Cosmos DB
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
#endregion

#region HTTP clients & app services
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<PaystackService>();
builder.Services.AddScoped<AzureSearchIndexManager>();
builder.Services.AddScoped<AzureAISearchService>();
builder.Services.AddScoped<WidgetTokenService>();
#endregion

var app = builder.Build();

#region Development-only initialization
if (app.Environment.IsDevelopment())
{
    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI();

    // Cosmos container initialization
    try
    {
        var cosmos = app.Services.GetRequiredService<CosmosDbService>();
        await cosmos.InitializeAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Cosmos initialization failed: {ex.Message}");
    }

    // Azure Search index (dev only)
    try
    {
        using var scope = app.Services.CreateScope();
        var indexManager = scope.ServiceProvider
            .GetRequiredService<AzureSearchIndexManager>();

        await indexManager.CreateSimpleIndexAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Search index creation failed: {ex.Message}");
    }
}
else
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "BizBot API v1");

        options.RoutePrefix = "swagger";
    });
}
#endregion

#region Global middleware
app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Order matters
app.UseCors("WidgetPolicy");
app.UseRateLimiter();

// CSP – allow iframe embedding ONLY from trusted domains
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] =
        "frame-ancestors https://widget.bizbot.space https://app.bizbot.space";
    await next();
});
#endregion

#region Endpoints
app.MapChatEndpoints();          // apply limiter inside endpoint group
app.MapSubscriptionEndpoints();
app.MapSearchEndpoints();
app.MapSettingsEndpoints();
app.MapWidgetEndpoints();
app.MapAdminEndpoints();
app.MapPaystackEndpoints();

app.MapGet("/", () => "BizBot API is running!");

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow
    }))
    .ExcludeFromDescription();

app.MapGet("/error", () =>
    Results.Problem("An unexpected error occurred."));
#endregion

app.Run();
