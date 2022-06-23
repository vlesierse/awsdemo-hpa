using RabbitMQ.Client;

namespace Infrastructure.Messaging.RabbitMQ;

public interface IRabbitMQPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect();

    IModel CreateModel();
}