using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SonicWallInterface.Configuration;
using SonicWallInterface.Consumers;
using SonicWallInterface.Services;
using Microsoft.Extensions.Logging;
using SonicWallInterface.Tests.Models;

namespace SonicWallInterface.Tests.Integration
{
    public abstract class TestBase
    {
        public IHost IHost {get; private set;}
        private CancellationTokenSource _sourceToken;

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
                var threatIntelApiConfig = context.Configuration.GetSection(nameof(ThreatIntelApiConfig)).Get<ThreatIntelApiConfig>();
                var sonicWallConfig = context.Configuration.GetSection(nameof(SonicWallConfig)).Get<SonicWallConfig>();
                if(!testConfig.UseMockServiceBus){
                    if(!serviceBusConfig.IsPresent) throw new Exception("No active Configuration found for service bus");
                    services.Configure<ServiceBusConfig>(context.Configuration.GetSection(nameof(ServiceBusConfig)));
                }
                if(!testConfig.UseMockTIApi){
                    if(!threatIntelApiConfig.IsPresent) throw new Exception("No active Configuration found for TI API");
                    services.Configure<ThreatIntelApiConfig>(context.Configuration.GetSection(nameof(ThreatIntelApiConfig)));
                    services.AddSingleton<IThreatIntelApi, ThreatIntelApi>();
                }
                else{
                    services.AddSingleton<ThreatIntelMockApi>(x => new ThreatIntelMockApi(x.GetRequiredService<ILogger<ThreatIntelMockApi>>(), tiIps));
                    services.AddSingleton<IThreatIntelApi>(x => x.GetService<ThreatIntelMockApi>());
                }
                if(!testConfig.UseMockSonicWall){
                    if(!sonicWallConfig.IsPresent) throw new Exception("No active Configuration found for Sonic Wall");
                    services.Configure<SonicWallConfig>(context.Configuration.GetSection(nameof(SonicWallConfig)));
                    services.AddSingleton<ISonicWallApi, SonicWallTIApi>();
                }
                else{
                    services.AddSingleton<SonicWallTIMockApi>(x => new SonicWallTIMockApi(x.GetRequiredService<ILogger<SonicWallTIMockApi>>(), x.GetRequiredService<IThreatIntelApi>(), bannedIps));
                    services.AddSingleton<ISonicWallApi>(x => x.GetService<SonicWallTIMockApi>());
                }
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<BlockIPsConsumer>(typeof(BlockIPsConsumerDefinition));
                    x.SetKebabCaseEndpointNameFormatter();
                    if(!testConfig.UseMockServiceBus)
                    {
                        x.UsingAzureServiceBus((messageContext, cfg) =>
                        {
                            cfg.Host(serviceBusConfig.ConnectionString);
                            cfg.ConfigureEndpoints(messageContext);
                        });
                    }
                    else
                    {
                        x.UsingInMemory((messageContext, cfg) =>
                        {
                            cfg.ConcurrentMessageLimit = 100;
                            cfg.ConfigureEndpoints(messageContext);
                        });
                    }
                });
                services.AddSingleton<TestWorker>();
            })
            .UseSerilog()
            .Build();
            Task.Run(async () => {
                await IHost.RunAsync(_sourceToken.Token);
            });
        }
    }

}