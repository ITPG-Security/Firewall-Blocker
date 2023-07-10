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
            await RunV2(args);
        }
        /*
        private static async Task Run(string[] args)
        {
            var appName = "Sonic Wall interface";
            var host = Host.CreateDefaultBuilder(args);
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                host = host.UseWindowsService(options =>
                {
                    options.ServiceName = appName;
                });
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                host = host.UseSystemd();
            }
            host.ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddConsole();
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    logging.AddEventLog(cfg =>
                    {
                        cfg.SourceName = appName;
                    });
                }
            });
            await host.ConfigureServices((context, services) =>
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);
                }

                services.Configure<ServiceBusConfig>(context.Configuration.GetSection(nameof(ServiceBusConfig)));
                services.Configure<SonicWallConfig>(context.Configuration.GetSection(nameof(SonicWallConfig)));
                services.Configure<ThreatIntelApiConfig>(context.Configuration.GetSection(nameof(ThreatIntelApiConfig)));

                if (context.Configuration.GetSection(nameof(SonicWallConfig)).Get<SonicWallConfig>().IsPresent)
                {
                    services.AddSingleton<IFireWallApi, SonicWallTIApi>();
                }
                else
                {
                    throw new Exception("Missing configuration: SonicWallConfig");
                }
                var tiConfig = context.Configuration.GetSection(nameof(ThreatIntelApiConfig)).Get<ThreatIntelApiConfig>();
                if (tiConfig.IsPresent && string.IsNullOrEmpty(tiConfig.WorkspaceId))
                {
                    services.AddSingleton<IThreatIntelApi, ThreatIntelApi>();
                }
                else if (tiConfig.IsPresent)
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
                if (!serviceBusConfig.IsPresent)
                {
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
            })
            .Build()
            .RunAsync();
        }
        */

        private static async Task RunV2(string[] args)
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
            builder.Services.Configure<ServiceBusConfig>(builder.Configuration.GetSection(nameof(ServiceBusConfig)));
            builder.Services.Configure<SonicWallConfig>(builder.Configuration.GetSection(nameof(SonicWallConfig)));
            builder.Services.Configure<ThreatIntelApiConfig>(builder.Configuration.GetSection(nameof(ThreatIntelApiConfig)));
            builder.Services.AddSingleton<IHttpIPListApi, HttpIPListApi>();
            if (builder.Configuration.GetSection(nameof(SonicWallConfig)).Get<SonicWallConfig>().IsPresent)
            {
                builder.Services.AddSingleton<IFireWallApi, SonicWallTIApi>();
            }
            var tiConfig = builder.Configuration.GetSection(nameof(ThreatIntelApiConfig)).Get<ThreatIntelApiConfig>();
            if (tiConfig.IsPresent && string.IsNullOrEmpty(tiConfig.WorkspaceId))
            {
                builder.Services.AddSingleton<IThreatIntelApi, ThreatIntelApi>();
            }
            else if (tiConfig.IsPresent)
            {
                builder.Services.AddSingleton<IThreatIntelApi, ThreatIntelLogAnalyticsApi>();
            }
            else
            {
                throw new Exception("Missing configuration: ThreatIntelApiConfig");
            }
            builder.Services.AddSingleton<ITIHandler, TIHandler>();

            var serviceBusConfig = builder.Configuration.GetSection(nameof(ServiceBusConfig)).Get<ServiceBusConfig>();
            var appConfig = builder.Configuration.GetSection(nameof(AppConfig)).Get<AppConfig>();
            if (!serviceBusConfig.IsPresent)
            {
                throw new Exception("Missing Messaging configuration");
            }
            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<BlockIPsConsumer>(typeof(BlockIPsConsumerDefinition));
                if (serviceBusConfig.IsPresent)
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