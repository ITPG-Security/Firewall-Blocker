using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using FirewallBlocker.Configuration;
using FirewallBlocker.Consumers;
using FirewallBlocker.Services;
using Messaging.Contracts;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace FirewallBlocker
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
            var srcIConfig = builder.Configuration.GetSection(nameof(SourceConfig));
            var firewallIConfig = builder.Configuration.GetSection(nameof(FirewallConfig));
            
            builder.Services.Configure<ServiceBusConfig>(serviceBusIConfig);
            builder.Services.Configure<FirewallConfig>(firewallIConfig);

            
            
            builder.Services.AddSingleton<IHttpIPListApi, HttpIPListApi>();
            
            

            var firewallConfig = firewallIConfig.Get<FirewallConfig>();
            if (!firewallIConfig.Exists() || !firewallConfig.IsPresent)
            {
                Console.WriteLine($"No FirewallConfig found. Direct firewall communication will not be supported!");
            }
            var srcConfig = srcIConfig.Get<SourceConfig>();
            if(srcConfig == null)
            {
                throw new Exception("No Source config present!");
            }
            if(srcConfig.CSVConfig != null && srcConfig.CSVConfig.IsPresent)
            {
                builder.Services.Configure<CSVConfig>(srcIConfig.GetSection(nameof(CSVConfig)));
                builder.Services.AddSingleton<IThreatIntelCollector, CSVHandler>();
            }
            else if(srcConfig.ThreatIntelApiConfig != null && srcConfig.ThreatIntelApiConfig.IsPresent)
            {
                builder.Services.Configure<ThreatIntelApiConfig>(srcIConfig.GetSection(nameof(ThreatIntelApiConfig)));
                var tiConfig = srcConfig.ThreatIntelApiConfig;
                if (tiConfig.IsPresent && string.IsNullOrEmpty(tiConfig.WorkspaceId))
                {
                    builder.Services.AddSingleton<IThreatIntelCollector, ThreatIntelApi>();
                }
                else if (tiConfig.IsPresent)
                {
                    builder.Services.AddSingleton<IThreatIntelCollector, ThreatIntelLogAnalyticsApi>();
                }
                else
                {
                    throw new Exception("Missing configuration: ThreatIntelApiConfig");
                }
            }
            else{
                throw new Exception("No valid Source config present!");
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