using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Options;
using ContentRus.Onboarding.Services;

/// <summary>
/// RabbitMQTenantStatusPublisher is responsible for publishing tenant status messages to a RabbitMQ queue.
/// </summary>
public class RabbitMQTenantStatusPublisher : IAsyncDisposable
{
    private const string tenant_status_queue_name = "tenant_status";
    private readonly ILogger<RabbitMQTenantStatusPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    /// <summary>
    /// Default constructor for RabbitMQTenantStatusPublisher.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    public RabbitMQTenantStatusPublisher(ILogger<RabbitMQTenantStatusPublisher> logger, IOptions<RabbitMqSettings> settings)
    {
        _logger = logger;

        var config = settings.Value;
        
        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            UserName = config.UserName,
            Password = config.Password,
        };

        // Since constructors can't be async, use `.Result` for quick setup (or refactor if needed)
        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;
    }

    /// <summary>
    /// Publishes a message to the RabbitMQ tenant status queue.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    public async Task PublishAsync<T>(T message)
    {
        _logger.LogInformation("Publishing message: {Message}", message);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.QueueDeclareAsync(
            queue: tenant_status_queue_name,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await _channel.BasicPublishAsync<BasicProperties>(
            exchange: "",
            routingKey: tenant_status_queue_name,
            mandatory: true,
            basicProperties: properties,
            body: body);
    }

    /// <summary>
    /// Disposes the RabbitMQ connection and channel.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();
    }
}
