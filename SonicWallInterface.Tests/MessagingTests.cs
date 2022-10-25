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
        public void MockEmptyBlockListTest(){
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
            var ips = sonicwallMock.GetIps();
            Assert.True(ips.All(ip => tiIps.Contains(ip)) && ips.Count == tiIps.Count);
        }
    }

}