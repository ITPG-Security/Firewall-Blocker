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

        public BlockIPsConsumer(ILogger<BlockIPsConsumer> logger, ISonicWallApi sonic)
        {
            _logger = logger;
            _sonic = sonic;
        }

        public async Task Consume(ConsumeContext<BlockIPs> context)
        {
            var payload = context.GetPayload<BlockIPs>();
            if (payload == null){
                _logger.Log(LogLevel.Error, "Payload not found. Message might be malformed");
                return;
            }
            _logger.Log(LogLevel.Information, "Digesting BlockIPs message created at {0} by: \"{1}\".", payload.DateTime, payload.CreatedBy);
        }
    }
}
