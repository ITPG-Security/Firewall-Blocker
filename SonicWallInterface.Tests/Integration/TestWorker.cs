using MassTransit;
using Messaging.Contracts;

namespace SonicWallInterface.Tests.Integration
{
    public class TestWorker
    {
        readonly IBus _bus;

        public TestWorker(IBus bus)
        {
            _bus = bus;
        }
        
        public async Task SendMessage(){
            var sendEndpoint = await _bus.GetSendEndpoint(new Uri("queue:ti-blocker"));
            await sendEndpoint.Send<BlockIPs>(new{ DateTime = DateTime.UtcNow, CreatedBy="Tester"});
        }
    }
}