using System.Text;
using System.Text.Json;
using GameFlow.Api.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GameFlow.Api.Services;

public sealed class RabbitMqPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqPublisher> logger) : IRabbitMqPublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.Username,
            Password = _options.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        channel.BasicPublish(exchange: string.Empty, routingKey: _options.QueueName, basicProperties: properties, body: payload);

        logger.LogInformation("Published message to RabbitMQ queue {QueueName} ({PayloadBytes} bytes).", _options.QueueName, payload.Length);
        return Task.CompletedTask;
    }
}
