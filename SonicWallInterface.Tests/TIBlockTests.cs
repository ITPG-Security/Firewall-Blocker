using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SonicWallInterface.Configuration;
using SonicWallInterface.Services;
using Xunit;

namespace SonicWallInterface.Tests
{
    [Collection("Sequential")]
    public class TIBlockTests
    {
        private readonly ILoggerFactory _logFactory;

        public TIBlockTests()
        {
            _logFactory = LoggerFactory.Create(x => x.AddConsole());
        }

        [Fact]
        public async Task EmptyBlockListTest(){
            var tiIps = new List<string>{
                "199.36.158.100",
                "31.11.32.79",
                "50.192.28.29",
                "67.225.140.4"
            };
            var sonicwallLogger = _logFactory.CreateLogger<SonicWallTIApi>();
            var httpLogger = _logFactory.CreateLogger<HttpIPListApi>();
            var sonicwallMock = (IFireWallApi) new SonicWallTIApi(sonicwallLogger, new SonicWallConfig{
                FireWallEndpoint = "https://127.0.0.1",
                Username = "admin",
                Password = "password",
                ValidateSSL = false
            }, new List<string>());
            var tiLogger = _logFactory.CreateLogger<ThreatIntelLogAnalyticsApi>();
            var tiMock = (IThreatIntelApi) new ThreatIntelLogAnalyticsApi(tiLogger, Options.Create<ThreatIntelApiConfig>(new ThreatIntelApiConfig{
                ClientId = "ClientId",
                TenantId = "TenantId",
                ClientSecret = "ClientSecret",
                WorkspaceId = "WorkspaceId",
                MinConfidence = 25
            }), tiIps);
            var tiHandlerLogger = _logFactory.CreateLogger<TIHandler>();
            var handler = (ITIHandler) new TIHandler(tiHandlerLogger, tiMock, new HttpIPListApi(httpLogger), new List<IFireWallApi>{sonicwallMock});
            await handler.HandleTI();
            var ips = await sonicwallMock.GetIPBlockList();
            Assert.True(ips.All(ip => tiIps.Contains(ip)) && ips.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips.Count}");
        }

        [Fact]
        public async Task FilledBlockListTest(){
            var tiIps = new List<string>{
                "199.36.158.100",
                "50.192.28.29",
                "67.225.140.4"
            };
            var oldIps = new List<string>{
                "31.11.32.79",
                "50.192.28.29",
                "67.225.140.4"
            };
            var sonicwallLogger = _logFactory.CreateLogger<SonicWallTIApi>();
            var httpLogger = _logFactory.CreateLogger<HttpIPListApi>();
            var sonicwallMock = (IFireWallApi) new SonicWallTIApi(sonicwallLogger, new SonicWallConfig{
                FireWallEndpoint = "https://127.0.0.1",
                Username = "admin",
                Password = "password",
                ValidateSSL = false
            }, oldIps);
            var tiLogger = _logFactory.CreateLogger<ThreatIntelLogAnalyticsApi>();
            var tiMock = (IThreatIntelApi) new ThreatIntelLogAnalyticsApi(tiLogger, Options.Create<ThreatIntelApiConfig>(new ThreatIntelApiConfig{
                ClientId = "ClientId",
                TenantId = "TenantId",
                ClientSecret = "ClientSecret",
                WorkspaceId = "WorkspaceId",
                MinConfidence = 25
            }), tiIps);
            var tiHandlerLogger = _logFactory.CreateLogger<TIHandler>();
            var handler = (ITIHandler) new TIHandler(tiHandlerLogger, tiMock, new HttpIPListApi(httpLogger), new List<IFireWallApi>{sonicwallMock});
            await handler.HandleTI();
            var ips = await sonicwallMock.GetIPBlockList();
            Assert.True(ips.All(ip => tiIps.Contains(ip)) && ips.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips.Count}");
        }

        [Fact]
        public async Task EmptyBlockListMultiTest(){
            var tiIps = new List<string>{
                "199.36.158.100",
                "31.11.32.79",
                "50.192.28.29",
                "67.225.140.4"
            };
            var sonicwallLogger = _logFactory.CreateLogger<SonicWallTIApi>();
            var httpLogger = _logFactory.CreateLogger<HttpIPListApi>();
            var sonicwallMock1 = (IFireWallApi) new SonicWallTIApi(sonicwallLogger, new SonicWallConfig{
                FireWallEndpoint = "https://127.0.0.1",
                Username = "admin",
                Password = "password",
                ValidateSSL = false
            }, new List<string>());
            var sonicwallMock2 = (IFireWallApi) new SonicWallTIApi(sonicwallLogger, new SonicWallConfig{
                FireWallEndpoint = "https://127.0.0.2",
                Username = "admin",
                Password = "password",
                ValidateSSL = false
            }, new List<string>());
            var tiLogger = _logFactory.CreateLogger<ThreatIntelLogAnalyticsApi>();
            var tiMock = (IThreatIntelApi) new ThreatIntelLogAnalyticsApi(tiLogger, Options.Create<ThreatIntelApiConfig>(new ThreatIntelApiConfig{
                ClientId = "ClientId",
                TenantId = "TenantId",
                ClientSecret = "ClientSecret",
                WorkspaceId = "WorkspaceId",
                MinConfidence = 25
            }), tiIps);
            var tiHandlerLogger = _logFactory.CreateLogger<TIHandler>();
            var handler = (ITIHandler) new TIHandler(tiHandlerLogger, tiMock, new HttpIPListApi(httpLogger), new List<IFireWallApi>{sonicwallMock1,sonicwallMock2});
            await handler.HandleTI();
            var ips1 = await sonicwallMock1.GetIPBlockList();
            var ips2 = await sonicwallMock2.GetIPBlockList();
            Assert.True(ips1.All(ip => tiIps.Contains(ip)) && ips1.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips1.Count}");
            Assert.True(ips2.All(ip => tiIps.Contains(ip)) && ips2.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips2.Count}");
        }

    }

}