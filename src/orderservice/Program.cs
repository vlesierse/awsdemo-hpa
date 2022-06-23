// Create Builder
using System.Net.Security;
using Amazon.SimpleNotificationService;
using Infrastructure.Messaging;
using Infrastructure.Messaging.RabbitMQ;
using Infrastructure.Messaging.SNS;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Orders.ServiceApi.Infrastructure;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);
// Configure Configuration
builder.Configuration.AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true);
// Configure Services
builder
    .Services.AddSingleton<HealthStore>()
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck<CustomHealthCheck>("custom");

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "orderservice", Version = "v1" });
});
builder.Services.AddSNS(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);
// Build Application
var app = builder.Build();
// Setup Application Pipeline
if (app.Environment.IsDevelopment())
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

app.Run();

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddSNS(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("SNS");
        if (!section.Exists())
        {
            return services;
        }
        services
            .Configure<SNSTopicOptions>(section)
            .AddDefaultAWSOptions(configuration.GetAWSOptions())
            .AddAWSService<IAmazonSimpleNotificationService>()
            .AddTransient<ITopic, SNSTopic>();
        return services;
    }

    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RabbitMQ");
        if (!section.Exists() || string.IsNullOrEmpty(section["HostName"]))
        {
            return services;
        }
        // Add Health Check
        var sslOptions = new SslOption { Enabled = true };
        sslOptions.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
        services.AddHealthChecks()
            .AddRabbitMQ(
            $"amqp://{section["UserName"]}:{section["Password"]}@{section["HostName"]}:5671",
            sslOption: sslOptions,
            name: "rabbitmq-check",
            tags: new string[] { "rabbitmqbus" });
        // Add Service
        return services
        .AddTransient<ITopic, RabbitMQTopic>()
        .AddSingleton<IRabbitMQPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMQPersistentConnection>>();

            var factory = new ConnectionFactory()
            {
                HostName = section["HostName"],
                DispatchConsumersAsync = true
            };
            if (!string.IsNullOrEmpty(section["RabbitMQUserName"]))
            {
                factory.UserName = section["UserName"];
            }

            if (!string.IsNullOrEmpty(section["Password"]))
            {
                factory.Password = section["Password"];
            }
            factory.Ssl = sslOptions;
            factory.Port = 5671;

            var retryCount = 5;
            if (!string.IsNullOrEmpty(section["RetryCount"]))
            {
                retryCount = int.Parse(section["RetryCount"]);
            }

            return new RabbitMQPersistentConnection(factory, logger, retryCount);
        });
    }
}