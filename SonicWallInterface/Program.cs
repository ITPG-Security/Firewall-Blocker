using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SonicWallInterface.Configuration;
using SonicWallInterface.Consumers;

namespace SonicWallInterface
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", false, true);
            var env = Environment.GetEnvironmentVariable("SONIC_INT__ENVIRONMENT");
            if(env != null && env.StartsWith("DEV")){
                configurationBuilder.AddUserSecrets("9a29c872-302c-4fb3-baea-c9b01650ed6e");
            }
            var config = configurationBuilder.Build();
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    context.Configuration = config;
                    var serviceBusConfig = context.Configuration.GetSection(nameof(ServiceBusConfig)).Get<ServiceBusConfig>();
                    services.Configure<ServiceBusConfig>(context.Configuration.GetSection(nameof(ServiceBusConfig)));
                    services.Configure<SonicWallConfig>(context.Configuration.GetSection(nameof(SonicWallConfig)));
                    services.Configure<ThreatIntelApiConfig>(context.Configuration.GetSection(nameof(ThreatIntelApiConfig)));
                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<BlockIPsConsumer>();

                        if (serviceBusConfig.IsPresent)
                        {
                            x.UsingAzureServiceBus((messageContext, cfg) =>
                            {

                                cfg.Host(serviceBusConfig.ConnectionString);
                                cfg.ReceiveEndpoint("fw-blocker", e =>
                                {
                                    e.UseMessageRetry(r =>
                                    {
                                        r.Interval(3, TimeSpan.FromMilliseconds(100));
                                    });

                                    e.ConfigureConsumer<BlockIPsConsumer>(messageContext, c => c.UseMessageRetry(r =>
                                    {
                                        r.Interval(3, TimeSpan.FromMilliseconds(200));
                                        //TODO: Add ignore exceptions
                                        //r.Ignore<SOME EXCEPTION HERE>();
                                    }));
                                });
                            });
                        }
                        else
                        {
                            x.UsingInMemory((messageContext, cfg) =>
                            {
                                cfg.ConcurrentMessageLimit = 100;
                            });
                        }
                    });
                })
                .UseSerilog()
                .Build()
                .RunAsync();
        }
    }
}