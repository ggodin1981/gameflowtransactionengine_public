using GameFlow.Shared.Persistence;

namespace GameFlow.Api.Services;

public sealed class DatabaseInitializationHostedService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseInitializationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GameFlowDbContext>();

                await DatabaseBootstrapper.InitializeAsync(dbContext, logger, stoppingToken);
                logger.LogInformation("Database initialization completed successfully.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    exception,
                    "Database initialization attempt {Attempt} of {MaxAttempts} failed. Retrying in 3 seconds.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Database initialization failed after {MaxAttempts} attempts.", maxAttempts);
                return;
            }
        }
    }
}
