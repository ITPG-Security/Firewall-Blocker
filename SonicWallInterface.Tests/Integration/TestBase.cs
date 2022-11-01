using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SonicWallInterface.Configuration;
using SonicWallInterface.Consumers;
using SonicWallInterface.Services;
using Microsoft.Extensions.Logging;
using SonicWallInterface.Tests.Models;
using Microsoft.Extensions.Options;
using Messaging.Contracts;
using MassTransit.Transports.Fabric;

namespace SonicWallInterface.Tests.Integration
{
    public abstract class TestBase
    {
        public IHost IHost {get; private set;}
        private CancellationTokenSource _sourceToken;

        public void StartHost(TestConfigModel testConfig){
            StartHost(new List<string>(), testConfig);
        }

        public void StartHost(List<string> tiIps, TestConfigModel testConfig)
        {
            StartHost(new List<string>(), tiIps, testConfig);
        }

        public void StartHost(List<string> bannedIps, List<string> tiIps, TestConfigModel testConfig){
            if(_sourceToken != null){
                _sourceToken.Cancel();
                _sourceToken.Dispose();
            }
            _sourceToken = new CancellationTokenSource();
            var config = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddUserSecrets("9a29c872-302c-4fb3-baea-c9b01650ed6e")
            .Build();
            IHost = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                context.Configuration = config;
                var serviceBusConfig = context.Configuration.GetSection(nameof(ServiceBusConfig)).Get<ServiceBusConfig>();
                var appConfig = context.Configuration.GetSection(nameof(AppConfig)).Get<AppConfig>();
                if(!appConfig.IsPresent) throw new Exception("No site name set");
                var threatIntelApiConfig = context.Configuration.GetSection(nameof(ThreatIntelApiConfig)).Get<ThreatIntelApiConfig>();
                var sonicWallConfig = context.Configuration.GetSection(nameof(SonicWallConfig)).Get<SonicWallConfig>();
                if(!testConfig.UseMockServiceBus){
                    if(!serviceBusConfig.IsPresent) throw new Exception("No active Configuration found for service bus");
                    services.Configure<ServiceBusConfig>(context.Configuration.GetSection(nameof(ServiceBusConfig)));
                }
                if(!testConfig.UseMockTIApi){
                    if(threatIntelApiConfig.IsPresent && string.IsNullOrEmpty(threatIntelApiConfig.WorkspaceId))
                    {
                        services.Configure<ThreatIntelApiConfig>(context.Configuration.GetSection(nameof(ThreatIntelApiConfig)));
                        services.AddSingleton<IThreatIntelApi, ThreatIntelApi>();
                    }
                    else if(threatIntelApiConfig.IsPresent)
                    {
                        services.Configure<ThreatIntelApiConfig>(context.Configuration.GetSection(nameof(ThreatIntelApiConfig)));
                        services.AddSingleton<IThreatIntelApi, ThreatIntelLogAnalyticsApi>();
                    }
                    else
                    {
                        throw new Exception("No active Configuration found for TI API");
                    }
                }
                else{
                    if(threatIntelApiConfig.IsPresent){
                        services.Configure<ThreatIntelApiConfig>(context.Configuration.GetSection(nameof(ThreatIntelApiConfig)));
                    }
                    else {
                        context.Configuration.Bind(nameof(ThreatIntelApiConfig), new ThreatIntelApiConfig{
                            ClientId = "ClientID",
                            ClientSecret = "SECRET",
                            TenantId = "TennantID",
                            WorkspaceId = "WorkspaceID",
                            MinConfidence = 25
                        });
                        services.Configure<ThreatIntelApiConfig>(context.Configuration.GetSection(nameof(ThreatIntelApiConfig)));
                    }
                    services.AddSingleton<ThreatIntelLogAnalyticsApi>(x => new ThreatIntelLogAnalyticsApi(x.GetRequiredService<ILogger<ThreatIntelLogAnalyticsApi>>(), x.GetRequiredService<IOptions<ThreatIntelApiConfig>>(), tiIps));
                    services.AddSingleton<IThreatIntelApi>(x => x.GetRequiredService<ThreatIntelLogAnalyticsApi>());
                }
                if(!testConfig.UseMockSonicWall){
                    if(!sonicWallConfig.IsPresent) throw new Exception("No active Configuration found for Sonic Wall");
                    services.Configure<SonicWallConfig>(context.Configuration.GetSection(nameof(SonicWallConfig)));
                    services.AddSingleton<ISonicWallApi, SonicWallTIApi>();
                }
                else{
                    services.AddSingleton<SonicWallTIMockApi>(x => new SonicWallTIMockApi(x.GetRequiredService<ILogger<SonicWallTIMockApi>>(), x.GetRequiredService<IThreatIntelApi>(), bannedIps));
                    services.AddSingleton<ISonicWallApi>(x => x.GetRequiredService<SonicWallTIMockApi>());
                }
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<BlockIPsConsumer>(typeof(BlockIPsConsumerDefinition));
                    if(!testConfig.UseMockServiceBus)
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
                    else
                    {
                        x.SetKebabCaseEndpointNameFormatter();
                        x.UsingInMemory((messageContext, cfg) =>
                        {
                            cfg.ConcurrentMessageLimit = 1;
                            
                            cfg.ConfigureEndpoints(messageContext, new KebabCaseEndpointNameFormatter("ti-blocker", false));
                            cfg.Message<BlockIPs>(m => m.SetEntityName("ti-blocker"));
                            cfg.Publish<BlockIPs>(p => {
                                p.ExchangeType = ExchangeType.Direct;
                            });
                        });
                    }
                });
                services.AddSingleton<TestWorker>();
            })
            .Build();
            Task.Run(async () => {
                await IHost.RunAsync(_sourceToken.Token);
            });
        }
    }

}