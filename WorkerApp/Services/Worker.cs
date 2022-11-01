using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Messaging.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkerApp.Services
{
    public class Worker : BackgroundService
    {
        private readonly IBus _bus;
        private readonly ILogger<Worker> _logger;
        
        public Worker(IBus bus, ILogger<Worker> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Log(LogLevel.Information, "Sending Message to service bus");
                await _bus.Publish<BlockIPs>(new { DateTime = DateTime.UtcNow, CreatedBy = "WorkerApp"}, stoppingToken);
                await Task.Delay(30000, stoppingToken);
            }
        }
    }
}