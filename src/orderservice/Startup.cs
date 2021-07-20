using System.Net.Security;
using Infrastructure.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Orders.ServiceApi.Infrastructure;
using RabbitMQ.Client;

namespace Orders.ServiceApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var sslOptions = new SslOption { Enabled = true };
            sslOptions.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
            services.AddSingleton<HealthStore>().AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddCheck<CustomHealthCheck>("custom")
                .AddRabbitMQ(
                        $"amqp://{Configuration["RabbitMQUserName"]}:{Configuration["RabbitMQPassword"]}@{Configuration["RabbitMQHostName"]}:5671",
                        sslOption: sslOptions,
                        name: "rabbitmq-check",
                        tags: new string[] { "rabbitmqbus" });


            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "orderservice", Version = "v1"});
            });
            services.AddRabbitMQ(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "orderservice v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapHealthChecks("/health/startup", new HealthCheckOptions { Predicate = _ => false });
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = _ => false });
                endpoints.MapControllers();
            });
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RabbitMQPersistentConnection>>();

                var factory = new ConnectionFactory()
                {
                    HostName = configuration["RabbitMQHostName"],
                    DispatchConsumersAsync = true
                };
                if (!string.IsNullOrEmpty(configuration["RabbitMQUserName"]))
                {
                    factory.UserName = configuration["RabbitMQUserName"];
                }

                if (!string.IsNullOrEmpty(configuration["RabbitMQPassword"]))
                {
                    factory.Password = configuration["RabbitMQPassword"];
                }
                factory.Port = 5671;
                factory.Ssl.Enabled = true;
                factory.Ssl.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;

                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["RabbitMQRetryCount"]))
                {
                    retryCount = int.Parse(configuration["RabbitMQRetryCount"]);
                }

                return new RabbitMQPersistentConnection(factory, logger, retryCount);
            });
        }
    }
}