using MassTransit;
using Messaging.Contracts;
using Microsoft.Extensions.Logging;
using FirewallBlocker.Exceptions;
using FirewallBlocker.Helpers;
using FirewallBlocker.Services;

namespace FirewallBlocker.Consumers
{
    public class BlockIPsConsumer : IConsumer<BlockIPs>
    {
        private readonly ILogger<BlockIPsConsumer> _logger;
        private readonly ITIHandler _tiHandler;
        private readonly IThreatIntelApi _threat;

        public BlockIPsConsumer(ILogger<BlockIPsConsumer> logger, ITIHandler tiHandler)
        {
            _logger = logger;
            _tiHandler = tiHandler;
        }

        public async Task Consume(ConsumeContext<BlockIPs> context)
        {
            if (context.Message == null){
                _logger.Log(LogLevel.Error, Events.Error, "Payload not found. Message might be malformed");
                return;
            }
            _logger.Log(LogLevel.Information, Events.Read, "Digesting BlockIPs message created at {0} by: \"{1}\".", context.Message.DateTime, context.Message.CreatedBy);
            try{
                await _tiHandler.HandleTI();
            }
            catch(Exception e){
                _logger.Log(LogLevel.Error, Events.Error, "An error occured during the handling of the message. \"{0}\"", e.Message);
                throw e;
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
                r.Interval(2, TimeSpan.FromSeconds(10));
                r.Ignore<UnreachableException>();
                r.Ignore<HttpRequestException>();
            });
        }
    }
}