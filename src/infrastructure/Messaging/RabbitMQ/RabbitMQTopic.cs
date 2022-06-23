using System.Text;
using System.Text.Json;

namespace Infrastructure.Messaging.RabbitMQ;

public class RabbitMQTopic : ITopic
{
    private readonly IRabbitMQPersistentConnection _connection;

    public RabbitMQTopic(IRabbitMQPersistentConnection connection)
    {
        _connection = connection;
    }
    public Task Publish<T>(T message)
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }
        using (var channel = _connection.CreateModel())
        {
            channel.QueueDeclare(queue: "orders",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            channel.BasicPublish(
                exchange: "",
                routingKey: "orders",
                mandatory: false,
                basicProperties: null,
                body: body);
        }
        return Task.CompletedTask;
    }
}