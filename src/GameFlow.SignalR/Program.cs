using GameFlow.SignalR.Hubs;
using GameFlow.SignalR.Options;
using GameFlow.SignalR.Services;
using System.Text.Json.Serialization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.Configure<InternalAuthOptions>(builder.Configuration.GetSection(InternalAuthOptions.SectionName));
builder.Services.AddSingleton<ConnectionRegistry>();
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors();
app.MapControllers();
app.MapHub<TransactionHub>("/hubs/transactions");
app.MapGet("/", () => Results.Ok(new
{
    service = "GameFlow.SignalR",
    status = "running",
    hub = "/hubs/transactions",
    health = new[]
    {
        "/health/live",
        "/health/ready"
    }
}));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.Run();
