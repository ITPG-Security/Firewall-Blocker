using MassTransit;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using SonicWallInterface.Exceptions;
using SonicWallInterface.Services;

namespace SonicWallInterface.Consumers
{
    public class BlockIPsConsumer : IConsumer<BlockIPs>
    {
        private readonly ILogger<BlockIPsConsumer> _logger;
        private readonly ISonicWallApi _sonic;
        private readonly IThreatIntelApi _threat;

        public BlockIPsConsumer(ILogger<BlockIPsConsumer> logger, ISonicWallApi sonic, IThreatIntelApi threat)
        {
            _logger = logger;
            _sonic = sonic;
            _threat = threat;
        }

        public async Task Consume(ConsumeContext<BlockIPs> context)
        {
            if (context.Message == null){
                _logger.Log(LogLevel.Error, "Payload not found. Message might be malformed");
                return;
            }
            _logger.Log(LogLevel.Information, "Digesting BlockIPs message created at {0} by: \"{1}\".", context.Message.DateTime, context.Message.CreatedBy);
            _logger.Log(LogLevel.Information, "Start to gather TI from Sentinel");
            var getTiTask = _threat.GetCurrentTIIPs();
            var currentIps = await _sonic.GetIPBlockList();
            if(currentIps.Count <= 0)
            {
                var tiIps = await getTiTask;
                await _sonic.InitiateIPBlockList(tiIps);
            }
            else
            {
                _logger.Log(LogLevel.Information, "BlockList exists. Updating block list.");
                var tiIps = await getTiTask;
                var removeIps = currentIps.Where(ip => !tiIps.Contains(ip)).ToList();
                var addIps = tiIps.Where(ip => !currentIps.Contains(ip)).ToList();
                await _sonic.RemoveFromIPBlockList(removeIps);
                await _sonic.AddToIPBlockList(addIps);
            }
        }
    }
    
    public class BlockIPsConsumerDefinition : ConsumerDefinition<BlockIPsConsumer>
    {
        public BlockIPsConsumerDefinition()
        {
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<BlockIPsConsumer> consumerConfigurator)
        {
            consumerConfigurator.UseMessageRetry(r => {
                r.Interval(2, TimeSpan.FromMilliseconds(200));
                r.Ignore<UnreachableException>();
            });
        }
    }
}