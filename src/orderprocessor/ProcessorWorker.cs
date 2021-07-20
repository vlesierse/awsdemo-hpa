using System;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Processor
{
    public class ProcessorWorker : IHostedService
    {
        private IModel _channel = null;
        private IConnection _connection = null;
        private IConfiguration _configuration = null;
        
        public ProcessorWorker(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Initiate RabbitMQ and start listening to an input queue
		private void Run()
        {
            // ! Fill in your data here !
			var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHostName"],
                UserName = _configuration["RabbitMQUserName"],
                Password = _configuration["RabbitMQPassword"],
                Port = 5671
            };
            factory.Ssl.Enabled = true;
            factory.Ssl.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;

            this._connection = factory.CreateConnection();
            this._channel = this._connection.CreateModel();
            
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
}