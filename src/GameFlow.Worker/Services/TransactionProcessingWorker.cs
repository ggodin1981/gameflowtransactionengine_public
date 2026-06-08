using System.Text.Json;
using GameFlow.Worker.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GameFlow.Worker.Services;

public sealed class TransactionProcessingWorker(
    IServiceProvider serviceProvider,
    IOptions<RabbitMqOptions> options,
    ILogger<TransactionProcessingWorker> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    VirtualHost = _options.VirtualHost,
                    UserName = _options.Username,
                    Password = _options.Password,
                    DispatchConsumersAsync = true
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (_, args) =>
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize<GameFlow.Shared.Messaging.TransactionCommandMessage>(args.Body.ToArray());
                        if (message is null)
                        {
                            throw new InvalidOperationException("Received null transaction command message.");
                        }

                        using var scope = serviceProvider.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<TransactionProcessor>();
                        await processor.ProcessAsync(message, stoppingToken);
                        channel.BasicAck(args.DeliveryTag, multiple: false);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Failed to process RabbitMQ delivery {DeliveryTag}.", args.DeliveryTag);
                        channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                channel.BasicConsume(queue: _options.QueueName, autoAck: false, consumer: consumer);
                logger.LogInformation("Worker is consuming queue {QueueName}.", _options.QueueName);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Worker consumption loop crashed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
