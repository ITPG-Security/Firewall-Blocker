using SonicWallInterface.Services;
using SonicWallInterface.Tests.Integration;
using SonicWallInterface.Tests.Models;
using Xunit;

namespace SonicWallInterface.Tests
{
    [Collection("Sequential")]
    public class MessagingTests : TestBase
    {
        [Fact]
        public void SB_M__TI_M__SO_M_EmptyBlockListTest(){
            var tiIps = new List<string>{
                "199.36.158.100",
                "31.11.32.79",
                "50.192.28.29",
                "67.225.140.4"
            };
            var testConfig = new TestConfigModel{
                UseMockServiceBus = true,
                UseMockSonicWall = true,
                UseMockTIApi = true
            };
            this.StartHost(tiIps, testConfig);
            var worker = (TestWorker?) IHost.Services.GetService(typeof(TestWorker));
            Assert.NotNull(worker);
            worker.SendMessage().Wait();
            Task.Delay(200).Wait();
            var sonicwallMock = (SonicWallTIMockApi?) IHost.Services.GetService(typeof(SonicWallTIMockApi));
            var ips = sonicwallMock.IpAddresses;
            Assert.True(ips.All(ip => tiIps.Contains(ip)) && ips.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips.Count}");
        }

        [Fact]
        public void SB_M__TI_M__SO_M_FilledBlockListTest(){
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
            var testConfig = new TestConfigModel{
                UseMockServiceBus = true,
                UseMockSonicWall = true,
                UseMockTIApi = true
            };
            this.StartHost(oldIps, tiIps, testConfig);
            var sonicwallMock = (SonicWallTIMockApi?) IHost.Services.GetService(typeof(SonicWallTIMockApi));
            var ips = sonicwallMock.IpAddresses;
            Assert.True(ips.All(ip => oldIps.Contains(ip)) && ips.Count == oldIps.Count);
            var worker = (TestWorker?) IHost.Services.GetService(typeof(TestWorker));
            Assert.NotNull(worker);
            worker.SendMessage().Wait();
            Task.Delay(200).Wait();
            ips = sonicwallMock.IpAddresses;
            Assert.True(ips.All(ip => tiIps.Contains(ip)) && ips.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips.Count}");
        }
        
        [Fact]
        public void SB_M__TI_M__SO_R_SonicWallIntegrationTest(){
            var tiIps = new List<string>{
                "199.36.158.100",
                "31.11.32.79",
                "50.192.28.29",
                "67.225.140.4"
            };
            var testConfig = new TestConfigModel{
                UseMockServiceBus = true,
                UseMockSonicWall = false,
                UseMockTIApi = true
            };
            this.StartHost(tiIps, testConfig);
            var worker = (TestWorker?) IHost.Services.GetService(typeof(TestWorker));
            Assert.NotNull(worker);
            worker.SendMessage().Wait();
            Task.Delay(200).Wait();
            var sonicwall = (ISonicWallApi?) IHost.Services.GetService(typeof(ISonicWallApi));
            var request = sonicwall.GetIPBlockList();
            var ctr = 0;
            while(!request.IsCompleted || ctr >= 1000){
                Task.Delay(10).Wait();
                ctr+=10;
            }
            var ips = request.Result;
            Assert.NotNull(ips);
            Assert.True(ips.All(ip => tiIps.Contains(ip)) && ips.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips.Count}");
        }

        [Fact]
        public void SB_M__TI_R__SO_M_TIIntegrationTest(){
            var testConfig = new TestConfigModel{
                UseMockServiceBus = true,
                UseMockSonicWall = true,
                UseMockTIApi = false
            };
            this.StartHost(testConfig);
            var worker = (TestWorker?) IHost.Services.GetService(typeof(TestWorker));
            Assert.NotNull(worker);
            var workerTask = worker.SendMessage();
            var tiApi = (IThreatIntelApi?) IHost.Services.GetService(typeof(IThreatIntelApi));
            Assert.NotNull(tiApi);
            var tiTask = tiApi.GetCurrentTIIPs();
            workerTask.Wait();
            var ctr = 0;
            while(!tiTask.IsCompleted || ctr >= 2000){
                Task.Delay(10).Wait();
                ctr+=10;
            }
            var tiIps = tiTask.Result;
            Task.Delay(200).Wait();
            var sonicwallMock = (SonicWallTIMockApi?) IHost.Services.GetService(typeof(SonicWallTIMockApi));
            Assert.NotNull(sonicwallMock);
            var ips = sonicwallMock.IpAddresses;
            Assert.True(ips.All(ip => tiIps.Contains(ip)) && ips.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips.Count}");
        }

        
        [Fact]
        public void SB_M__TI_R__SO_R_SonicWallIntegrationTest(){
            var testConfig = new TestConfigModel{
                UseMockServiceBus = true,
                UseMockSonicWall = false,
                UseMockTIApi = false
            };
            this.StartHost(testConfig);
            var worker = (TestWorker?) IHost.Services.GetService(typeof(TestWorker));
            Assert.NotNull(worker);
            worker.SendMessage().Wait();
            var tiApi = (IThreatIntelApi?) IHost.Services.GetService(typeof(IThreatIntelApi));
            Assert.NotNull(tiApi);
            var tiTask = tiApi.GetCurrentTIIPs();
            Task.Delay(200).Wait();
            var ctr = 0;
            while(!tiTask.IsCompleted && ctr <= 2000){
                Task.Delay(10).Wait();
                ctr+=10;
            }
            var sonicwall = (ISonicWallApi?) IHost.Services.GetService(typeof(ISonicWallApi));
            var sonicTask = sonicwall.GetIPBlockList();
            ctr = 0;
            while(!sonicTask.IsCompleted && ctr <= 1000){
                Task.Delay(10).Wait();
                ctr+=10;
            }
            var tiIps = tiTask.Result;
            var ips = sonicTask.Result;
            Assert.NotNull(ips);
            Assert.True(ips.All(ip => tiIps.Contains(ip)) && ips.Count == tiIps.Count, $"TI is not the same is SonicWall. TI count:{tiIps.Count} | Sonic count: {ips.Count}");
        }


    }

}