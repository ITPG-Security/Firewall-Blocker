using MassTransit;
using Messaging.Contracts;
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
            if (context.Message == null){
                _logger.Log(LogLevel.Error, "Payload not found. Message might be malformed");
                return;
            }
            _logger.Log(LogLevel.Information, "Digesting BlockIPs message created at {0} by: \"{1}\".", context.Message.DateTime, context.Message.CreatedBy);
            await _sonic.BlockIPsAsync();
        }
    }
    
    public class BlockIPsConsumerDefinition : ConsumerDefinition<BlockIPsConsumer>
    {
        public BlockIPsConsumerDefinition()
        {
            EndpointName = "ti-blocker";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<BlockIPsConsumer> consumerConfigurator)
        {
        }
    }
}