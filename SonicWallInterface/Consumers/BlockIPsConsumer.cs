using MassTransit;
using Messaging.Contracts;
using Microsoft.Azure.Amqp;
using Microsoft.Extensions.Logging;
using SonicWallInterface.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicWallInterface.Consumers
{
    public class BlockIPsConsumer : IConsumer<BlockIPs>
    {
        private readonly ILogger<BlockIPsConsumer> _logger;
        private ISessionFactory _sessionFactory;

        public BlockIPsConsumer(ILogger<BlockIPsConsumer> logger) 
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<BlockIPs> context)
        {
            _logger.Log(LogLevel.Information, "Digesting BlockIPs message.");
        }
    }
}
