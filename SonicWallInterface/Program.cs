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
            
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", false, true);
            if(Environment.GetEnvironmentVariable("SONIC_INT__ENVIRONMENT") != null && Environment.GetEnvironmentVariable("SONIC_INT__ENVIRONMENT").StartsWith("DEV")){
                configurationBuilder.AddUserSecrets("9a29c872-302c-4fb3-baea-c9b01650ed6e");
            }
            Configuration = configurationBuilder.Build();
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