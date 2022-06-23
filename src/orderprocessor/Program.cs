using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Processor;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((host, services) =>
    {
        services.AddHostedService<RabbitMQProcessorWorker>();
        services
            .Configure<SQSProcessorOptions>(host.Configuration.GetSection("SQS"))
            .AddDefaultAWSOptions(host.Configuration.GetAWSOptions())
            .AddAWSService<IAmazonSQS>()
            .AddHostedService<SQSProcessorWorker>();
    })
    .ConfigureAppConfiguration((contenxt, c) =>
    {
        c.AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true);
    })
    .Build();

host.Run();