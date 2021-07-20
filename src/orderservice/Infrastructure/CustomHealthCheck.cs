using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orders.ServiceApi.Infrastructure
{
    public class CustomHealthCheck : IHealthCheck
    {

        private readonly HealthStore _health;

        public CustomHealthCheck(HealthStore health)
        {
            _health = health;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<HealthCheckResult>(_health.Fail ? HealthCheckResult.Unhealthy("Health store set to fail!") : HealthCheckResult.Healthy());
        }
    }
}