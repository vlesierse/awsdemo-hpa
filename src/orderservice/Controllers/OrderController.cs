using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Infrastructure.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orders.ServiceApi.Infrastructure;
using Orders.ServiceApi.Model;

namespace Orders.ServiceApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IRabbitMQPersistentConnection _connection;
        private readonly ILogger<OrderController> _logger;
        private readonly HealthStore _health;

        public OrderController(IRabbitMQPersistentConnection connection, ILogger<OrderController> logger, HealthStore health)
        {
            _connection = connection;
            _logger = logger;
            _health = health;
        }

        [HttpGet("fail")]
        public ActionResult Fail()
        {
            _health.Fail = true;
            return Ok();
        }

        [HttpPost]
        public Order CreateOrder([FromBody]CreateOrder order)
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

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(order));

                channel.BasicPublish(
                    exchange: "",
                    routingKey: "orders",
                    mandatory: false,
                    basicProperties: null,
                    body: body);
                Console.WriteLine(" [x] Order {0}", order.OrderId);
            }

            return new Order() {OrderId = order.OrderId, OrderStatus = "Created"};
        }
    }
}