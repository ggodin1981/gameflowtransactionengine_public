using FluentValidation;
using FluentValidation.AspNetCore;
using GameFlow.Api.Options;
using GameFlow.Api.Services;
using GameFlow.Api.Validation;
using GameFlow.Shared.Contracts.Transactions;
using GameFlow.Shared.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Serilog;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.Configure<PostgresOptions>(builder.Configuration.GetSection(PostgresOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<DemoModeOptions>(builder.Configuration.GetSection(DemoModeOptions.SectionName));
builder.Services.Configure<SignalRServiceOptions>(builder.Configuration.GetSection(SignalRServiceOptions.SectionName));

builder.Services.AddDbContext<GameFlowDbContext>((serviceProvider, options) =>
{
    var postgres = serviceProvider.GetRequiredService<IConfiguration>()
        .GetSection(PostgresOptions.SectionName)
        .Get<PostgresOptions>() ?? new PostgresOptions();

    var connectionString = postgres.ConnectionString;
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Postgres connection string is not configured. Set Postgres__ConnectionString or provide it in appsettings.Local.json for local development.");
    }

    options.UseNpgsql(connectionString);
});

var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisOptions.ConnectionString;
        options.InstanceName = redisOptions.InstanceName;
    });
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionRequestValidator>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
            ?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            return;
        }

        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Accept proxy-forwarded headers from the hosting edge so per-IP limits remain meaningful behind Render/Cloudflare.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
            httpContext.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("health-and-swagger");
        }

        var partitionKey = GetRateLimitPartitionKey(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            $"global:{partitionKey}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("transaction-write", httpContext =>
    {
        var partitionKey = GetRateLimitPartitionKey(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            $"tx-write:{partitionKey}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddScoped<IValidator<CreateTransactionRequest>, CreateTransactionRequestValidator>();
builder.Services.AddScoped<ITransactionCommandService, TransactionCommandService>();
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();
builder.Services.AddScoped<IPlayerProfileService, PlayerProfileService>();
builder.Services.AddHostedService<DatabaseInitializationHostedService>();

var demoMode = builder.Configuration.GetSection(DemoModeOptions.SectionName).Get<DemoModeOptions>() ?? new DemoModeOptions();
if (demoMode.Enabled)
{
    builder.Services.AddSingleton<ITransactionProcessingQueue, InMemoryTransactionQueue>();
    builder.Services.AddSingleton<IRabbitMqPublisher, InMemoryTransactionPublisher>();
    builder.Services.AddScoped<DemoTransactionProcessor>();
    builder.Services.AddHttpClient<ITransactionLifecycleNotifier, SignalRLifecycleNotifier>((serviceProvider, httpClient) =>
    {
        var signalROptions = serviceProvider.GetRequiredService<IOptions<SignalRServiceOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(signalROptions.BaseUrl))
        {
            httpClient.BaseAddress = new Uri(signalROptions.BaseUrl);
            httpClient.DefaultRequestHeaders.Add("X-GameFlow-Key", signalROptions.ApiKey);
        }
    });
    builder.Services.AddHostedService<DemoTransactionProcessingWorker>();
}
else
{
    builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
}

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.UseForwardedHeaders();
app.UseCors();
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (GameFlowDbContext dbContext) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "ready" })
        : Results.Problem("Database connectivity check failed.");
});

app.Run();

static string GetRateLimitPartitionKey(HttpContext httpContext)
{
    if (httpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
    {
        return $"api-key:{apiKey.ToString()}";
    }

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
