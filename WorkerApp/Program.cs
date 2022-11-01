using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Messaging.Contracts;
using SonicWallInterface.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkerApp.Services;

namespace WorkerApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", false, true);
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            if(env != null && env.ToUpper().StartsWith("DEV")){
                configurationBuilder.AddUserSecrets("9a29c872-302c-4fb3-baea-c9b01650ed6e");
            }
            var config = configurationBuilder.Build();
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    hostContext.Configuration = config;
                    var serviceBusConfig = hostContext.Configuration.GetSection(nameof(ServiceBusConfig)).Get<ServiceBusConfig>();
                    if (!serviceBusConfig.IsPresent){
                        throw new Exception("Missing Service bus config!");
                    }
                    services.AddMassTransit(x =>
                    {
                        x.UsingAzureServiceBus((messageContext, cfg) =>
                        {
                            cfg.Host(serviceBusConfig.ConnectionString);
                            cfg.Message<BlockIPs>(m => {
                                m.SetEntityName("ti-blocker");
                            });
                        });
                    });
                    services.AddHostedService<Worker>();
                })
            .Build().RunAsync();
        }
    }
}
