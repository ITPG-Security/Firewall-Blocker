using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using SonicWallInterface.Configuration;
using SonicWallInterface.Consumers;

namespace SonicWallInterface
{
    public class Program
    {
        private static IConfiguration Configuration;

        public static async Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .Build();
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    var serviceBusConfig = Configuration.GetSection(nameof(ServiceBusConfig)).Get<ServiceBusConfig>();
                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<BlockIPsConsumer>();

                        if (serviceBusConfig.IsPresent)
                        {
                            x.UsingAzureServiceBus((context, cfg) =>
                            {

                                cfg.Host(serviceBusConfig.ConnectionString);
                                cfg.ReceiveEndpoint("fw-blocker", e =>
                                {
                                    e.UseMessageRetry(r =>
                                    {
                                        r.Interval(3, TimeSpan.FromMilliseconds(100));
                                    });

                                    e.ConfigureConsumer<BlockIPsConsumer>(context, c => c.UseMessageRetry(r =>
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
                            x.UsingInMemory((context, cfg) =>
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