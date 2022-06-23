using Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Orders.ServiceApi.Infrastructure;
using Orders.ServiceApi.Model;

namespace Orders.ServiceApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ITopic _topic;
        private readonly ILogger<OrderController> _logger;
        private readonly HealthStore _health;

        public OrderController(ITopic topic, ILogger<OrderController> logger, HealthStore health)
        {
            _topic = topic;
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
        public async Task<Order> CreateOrder([FromBody]CreateOrder order)
        {
            await _topic.Publish(order);
            Console.WriteLine(" [x] Order {0}", order.OrderId);
            return new Order() {OrderId = order.OrderId, OrderStatus = "Created"};
        }
    }
}