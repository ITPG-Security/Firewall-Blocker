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
            await _bus.Publish<BlockIPs>(new{ DateTime = DateTime.UtcNow, CreatedBy="Tester"});
        }
    }
}