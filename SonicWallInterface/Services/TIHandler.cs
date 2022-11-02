using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SonicWallInterface.Services
{
    public class TIHandler : ITIHandler
    {
        private readonly ILogger<TIHandler> _logger;
        private readonly ISonicWallApi _sonic;
        private readonly IThreatIntelApi _threat;

        public TIHandler(ILogger<TIHandler> logger, ISonicWallApi sonic, IThreatIntelApi threat)
        {
            _logger = logger;
            _sonic = sonic;
            _threat = threat;
        }

        public async Task HandleTI()
        {
            _logger.Log(LogLevel.Debug, "Gathering TI from Sentinel");
            var getTiTask = _threat.GetCurrentTIIPs();
            var currentIps = await _sonic.GetIPBlockList();
            if(currentIps.Count <= 0)
            {
                _logger.Log(LogLevel.Debug, "Initializing block list.");
                var tiIps = await getTiTask;
                await _sonic.InitiateIPBlockList(tiIps);
            }
            else
            {
                _logger.Log(LogLevel.Debug, "BlockList exists. Updating block list.");
                var tiIps = await getTiTask;
                var removeIps = currentIps.Where(ip => !tiIps.Contains(ip)).ToList();
                var addIps = tiIps.Where(ip => !currentIps.Contains(ip)).ToList();
                await _sonic.RemoveFromIPBlockList(removeIps);
                await _sonic.AddToIPBlockList(addIps);
            }
        }
    }
}