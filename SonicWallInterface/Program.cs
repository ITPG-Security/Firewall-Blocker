using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SonicWallInterface.Configuration;
using SonicWallInterface.Consumers;
using SonicWallInterface.Services;
using Messaging.Contracts;

namespace SonicWallInterface
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
                .ConfigureServices((context, services) =>
                {
                    context.Configuration = config;
                    services.Configure<ServiceBusConfig>(context.Configuration.GetSection(nameof(ServiceBusConfig)));
                    services.Configure<SonicWallConfig>(context.Configuration.GetSection(nameof(SonicWallConfig)));
                    services.Configure<ThreatIntelApiConfig>(context.Configuration.GetSection(nameof(ThreatIntelApiConfig)));
                    
                    if(context.Configuration.GetSection(nameof(SonicWallConfig)).Get<SonicWallConfig>().IsPresent)
                    {
                        services.AddSingleton<ISonicWallApi, SonicWallTIApi>();
                    }
                    else 
                    {
                        throw new Exception("Missing configuration: SonicWallConfig");
                    }
                    var tiConfig = context.Configuration.GetSection(nameof(ThreatIntelApiConfig)).Get<ThreatIntelApiConfig>();
                    if(tiConfig.IsPresent && string.IsNullOrEmpty(tiConfig.WorkspaceId))
                    {
                        services.AddSingleton<IThreatIntelApi, ThreatIntelApi>();
                    }
                    else if(tiConfig.IsPresent)
                    {
                        services.AddSingleton<IThreatIntelApi, ThreatIntelLogAnalyticsApi>();
                    }
                    else 
                    {
                        throw new Exception("Missing configuration: ThreatIntelApiConfig");
                    }
                    var serviceBusConfig = context.Configuration.GetSection(nameof(ServiceBusConfig)).Get<ServiceBusConfig>();
                    var appConfig = context.Configuration.GetSection(nameof(AppConfig)).Get<AppConfig>();
                    if(!serviceBusConfig.IsPresent){
                        throw new Exception("Missing Messaging configuration");
                    }
                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<BlockIPsConsumer>(typeof(BlockIPsConsumerDefinition));
                        if (serviceBusConfig.IsPresent)
                        {
                            x.UsingAzureServiceBus((messageContext, cfg) =>
                            {
                                cfg.Host(serviceBusConfig.ConnectionString);
                                cfg.SubscriptionEndpoint(appConfig.SiteName, "ti-blocker", e => {
                                    e.ConfigureConsumer<BlockIPsConsumer>(messageContext);
                                });
                                cfg.Message<BlockIPs>(m => {
                                    m.SetEntityName("ti-blocker");
                                });
                            });
                        }
                        else{
                            throw new Exception("No valid Messaging configured!");
                        }
                        //Future add rabbitMQ config
                    });
                })
                .Build()
                .RunAsync();
        }
    }
}