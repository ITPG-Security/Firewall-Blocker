using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using SonicWallInterface.Configuration;
using SonicWallInterface.Consumers;
using SonicWallInterface.Services;
using Messaging.Contracts;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace SonicWallInterface
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if(args.Any(a => a == "--version" || a == "-v"))
            {
                Console.WriteLine($"v{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}");
                return;
            }
            await Run(args);
        }

        private static async Task Run(string[] args)
        {
            var appName = "Sonic Wall interface";
            var host = Host.CreateDefaultBuilder(args);
            host.ConfigureHostConfiguration((cfg) => {
                cfg.AddJsonFile("appsettings.json", false, true);
                var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
                if(env != null && env.ToUpper().StartsWith("DEV")){
                    cfg.AddUserSecrets("9a29c872-302c-4fb3-baea-c9b01650ed6e");
                }
            });
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)){
                host = host.UseWindowsService(options => {
                    options.ServiceName = appName;
                });
                
            }
            else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                host = host.UseSystemd();
            }
            host.ConfigureLogging((context, logging) => {
                logging.ClearProviders();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddConsole();
                if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)){
                    logging.AddEventLog(cfg => {
                        cfg.SourceName = appName;
                    });
                }
            });
            await host.ConfigureServices((context, services) =>
            {
                if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)){
                    LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);
                }
                
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
                services.AddSingleton<ITIHandler, TIHandler>();
                
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
                                e.UseCircuitBreaker(cb => {
                                    cb.TrackingPeriod = TimeSpan.FromMinutes(10);
                                    cb.TripThreshold = 2;
                                    cb.ActiveThreshold = 10;
                                    cb.ResetInterval = TimeSpan.FromMinutes(15);
                                });
                                e.LockDuration = TimeSpan.FromMinutes(5);
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