using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using ContentRus.TenantManagement.RabbitMQ;
using Microsoft.Extensions.Options;

public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly string tenant_status_queue_name;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqPublisher(IOptions<RabbitMqSettings> settings, string queueName)
    {
        var config = settings.Value;
        
        tenant_status_queue_name = queueName ?? throw new ArgumentNullException(nameof(queueName));

        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            UserName = config.UserName,
            Password = config.Password
        };

        // Since constructors can't be async, use `.Result` for quick setup (or refactor if needed)
        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;
    }

    public async Task PublishAsync<T>(T message)
    {
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

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();
    }
}
