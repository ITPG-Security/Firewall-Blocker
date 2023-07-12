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
            if (args.Any(a => a == "--version" || a == "-v"))
            {
                Console.WriteLine($"v{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}");
                return;
            }
            await Run(args);
        }

        private static async Task Run(string[] args)
        {
            var appName = "Sonic Wall interface";
            var builder = WebApplication.CreateBuilder(args);
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) && !builder.Environment.IsDevelopment())
            {
                builder.Services.AddWindowsService(conf =>
                {
                    conf.ServiceName = appName;
                });
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux) && !builder.Environment.IsDevelopment())
            {
                builder.Services.AddSystemd();
            }
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) && !builder.Environment.IsDevelopment())
            {
                builder.Logging.AddEventLog(conf =>
                {
                    conf.LogName = appName;
                });
            }
            
            var serviceBusIConfig = builder.Configuration.GetSection(nameof(ServiceBusConfig));
            var appIConfig = builder.Configuration.GetSection(nameof(AppConfig));
            var tiIConfig = builder.Configuration.GetSection(nameof(ThreatIntelApiConfig));
            var sonicWallIConfig = builder.Configuration.GetSection(nameof(SonicWallConfig));
            
            builder.Services.Configure<ServiceBusConfig>(serviceBusIConfig);
            builder.Services.Configure<SonicWallConfig>(sonicWallIConfig);
            builder.Services.Configure<ThreatIntelApiConfig>(tiIConfig);
            
            builder.Services.AddSingleton<IHttpIPListApi, HttpIPListApi>();

            var sonicWallConfig = sonicWallIConfig.Get<SonicWallConfig>();
            if (sonicWallIConfig.Exists() && sonicWallConfig.IsPresent)
            {
                builder.Services.AddSingleton<IFireWallApi, SonicWallTIApi>();
            }
            var tiConfig = tiIConfig.Get<ThreatIntelApiConfig>();
            if (tiIConfig.Exists() && tiConfig.IsPresent && string.IsNullOrEmpty(tiConfig.WorkspaceId))
            {
                builder.Services.AddSingleton<IThreatIntelApi, ThreatIntelApi>();
            }
            else if (tiIConfig.Exists() && tiConfig.IsPresent)
            {
                builder.Services.AddSingleton<IThreatIntelApi, ThreatIntelLogAnalyticsApi>();
            }
            else
            {
                throw new Exception("Missing configuration: ThreatIntelApiConfig");
            }
            builder.Services.AddSingleton<ITIHandler, TIHandler>();
            
            var serviceBusConfig = serviceBusIConfig.Get<ServiceBusConfig>();
            var appConfig = appIConfig.Get<AppConfig>();
            if(!appIConfig.Exists() || !appConfig.IsPresent)
            {
                throw new Exception("Missing App configuration");
            }
            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<BlockIPsConsumer>(typeof(BlockIPsConsumerDefinition));
                if (serviceBusIConfig.Exists() && serviceBusConfig.IsPresent)
                {
                    x.UsingAzureServiceBus((messageContext, cfg) =>
                    {
                        cfg.Host(serviceBusConfig.ConnectionString);
                        cfg.SubscriptionEndpoint(appConfig.SiteName, "ti-blocker", e =>
                        {
                            e.ConfigureConsumer<BlockIPsConsumer>(messageContext);
                            e.UseCircuitBreaker(cb =>
                            {
                                cb.TrackingPeriod = TimeSpan.FromMinutes(10);
                                cb.TripThreshold = 2;
                                cb.ActiveThreshold = 10;
                                cb.ResetInterval = TimeSpan.FromMinutes(15);
                            });
                            e.LockDuration = TimeSpan.FromMinutes(5);
                        });
                        cfg.Message<BlockIPs>(m =>
                        {
                            m.SetEntityName("ti-blocker");
                        });
                    });
                }
                else
                {
                    throw new Exception("No valid Messaging configured!");
                }
                //Future add rabbitMQ config
            });
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var httpService = scope.ServiceProvider.GetRequiredService<IHttpIPListApi>();
                app.MapGet("/", () => httpService.GetIPBlockList());
            }

            await app.RunAsync();
        }
    }
}