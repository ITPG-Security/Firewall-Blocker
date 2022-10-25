using MassTransit;
using Messaging.Contracts;
using Microsoft.Azure.Amqp;
using Microsoft.Extensions.Logging;
using SonicWallInterface.Services;

namespace SonicWallInterface.Consumers
{
    public class BlockIPsConsumer : IConsumer<BlockIPs>
    {
        private readonly ILogger<BlockIPsConsumer> _logger;
        private readonly ISonicWallApi _sonic;
        private readonly IThreatIntelApi _threat;
        private ISessionFactory _sessionFactory;

        public BlockIPsConsumer(ILogger<BlockIPsConsumer> logger, ISonicWallApi sonic)
        {
            _logger = logger;
            _sonic = sonic;
        }

        public async Task Consume(ConsumeContext<BlockIPs> context)
        {
            _logger.Log(LogLevel.Information, "Digesting BlockIPs message.");
        }
    }
}
