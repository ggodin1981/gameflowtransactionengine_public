using GameFlow.Shared.Persistence;
using GameFlow.Worker.Options;
using GameFlow.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddSerilog((serviceProvider, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(serviceProvider)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.Configure<PostgresOptions>(builder.Configuration.GetSection(PostgresOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<SignalRServiceOptions>(builder.Configuration.GetSection(SignalRServiceOptions.SectionName));
builder.Services.Configure<ElasticSearchOptions>(builder.Configuration.GetSection(ElasticSearchOptions.SectionName));

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

builder.Services.AddHttpClient<ISignalRDispatcher, SignalRDispatchClient>((serviceProvider, httpClient) =>
{
    var signalROptions = serviceProvider.GetRequiredService<IOptions<SignalRServiceOptions>>().Value;
    httpClient.BaseAddress = new Uri(signalROptions.BaseUrl);
    httpClient.DefaultRequestHeaders.Add("X-GameFlow-Key", signalROptions.ApiKey);
});

builder.Services.AddHttpClient<ISearchIndexWriter, ElasticSearchIndexWriter>((serviceProvider, httpClient) =>
{
    var elasticSearchOptions = serviceProvider.GetRequiredService<IOptions<ElasticSearchOptions>>().Value;
    httpClient.BaseAddress = new Uri(elasticSearchOptions.BaseUrl);
});

builder.Services.AddScoped<TransactionProcessor>();
builder.Services.AddHostedService<TransactionProcessingWorker>();

var host = builder.Build();
await host.RunAsync();
