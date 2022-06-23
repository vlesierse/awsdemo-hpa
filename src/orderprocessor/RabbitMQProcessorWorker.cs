using System.Net.Security;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Processor;

public class RabbitMQProcessorWorker : IHostedService
{
    private IModel _channel = null;
    private IConnection _connection = null;
    private IConfiguration _configuration = null;
    private ILogger<RabbitMQProcessorWorker> _logger = null;

    public RabbitMQProcessorWorker(IConfiguration configuration, ILogger<RabbitMQProcessorWorker> logger)
    {
        _logger = logger;
        _configuration = configuration;
    }

    // Initiate RabbitMQ and start listening to an input queue
    private void Run()
    {
        var section = _configuration.GetSection("RabbitMQ");
        if (!section.Exists() || string.IsNullOrEmpty(section["HostName"]))
        {
            _logger.LogInformation("RabbitMQ Configuration missing. Not listening to RabbitMQ");
            return;
        }
        // ! Fill in your data here !
        var factory = new ConnectionFactory()
        {
            HostName = section["HostName"],
            UserName = section["UserName"],
            Password = section["Password"],
            Port = 5671
        };
        factory.Ssl.Enabled = true;
        factory.Ssl.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;

        this._connection = factory.CreateConnection();
        this._channel = this._connection.CreateModel();

        _logger.LogInformation("RabbitMQ Configuration missing. Not listening to RabbitMQ");
        Console.WriteLine(" [*] Waiting for messages.");

        var consumer = new EventingBasicConsumer(this._channel);
        consumer.Received += OnMessageRecieved;

        this._channel.BasicConsume(queue: "orders",
                            autoAck: false,
                            consumer: consumer);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.Run();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this._channel.Dispose();
        this._connection.Dispose();
        return Task.CompletedTask;
    }

    // Publish a received  message with "reply:" prefix
    private void OnMessageRecieved(object model, BasicDeliverEventArgs args)
    {
        var body = args.Body;
        var message = Encoding.UTF8.GetString(body.ToArray());
        var order = JsonSerializer.Deserialize<Order>(message);
        Console.WriteLine(" [x] Order Received {0}", order.OrderId);
        Thread.Sleep(1000);
        Console.WriteLine(" [x] Order Processed {0}", order.OrderId);
        this._channel.BasicAck(deliveryTag: args.DeliveryTag, multiple: false);
    }
}