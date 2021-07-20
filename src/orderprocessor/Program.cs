using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Processor;

IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<ProcessorWorker>();
        })
        .ConfigureAppConfiguration(c => 
        {
            c.AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true);
        });

await CreateHostBuilder(Environment.GetCommandLineArgs()).Build().RunAsync();
