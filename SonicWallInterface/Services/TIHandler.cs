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
        private readonly IFireWallApi _fireWall;
        private readonly IThreatIntelApi _threat;
        private readonly IHttpIPListApi _httpIps;

        public TIHandler(ILogger<TIHandler> logger, IFireWallApi fireWall, IThreatIntelApi threat, IHttpIPListApi httpIps)
        {
            _logger = logger;
            _fireWall = fireWall;
            _threat = threat;
            _httpIps = httpIps;
        }
        public TIHandler(ILogger<TIHandler> logger, IThreatIntelApi threat, IHttpIPListApi httpIps)
        {
            _logger = logger;
            _fireWall = null;
            _threat = threat;
            _httpIps = httpIps;
        }

        public async Task HandleTI()
        {
            _logger.Log(LogLevel.Debug, "Gathering TI from Sentinel");
            var getTiTask = _threat.GetCurrentTIIPs();
            var currentIps = _fireWall != null ? await _fireWall.GetIPBlockList() : new List<string>();
            var tiIps = await getTiTask;
            _httpIps.OverwriteIPBlockList(tiIps);
            if (_fireWall != null)
            {
                if (currentIps.Count <= 0)
                {
                    _logger.Log(LogLevel.Debug, "Initializing block list.");
                    await _fireWall.InitiateIPBlockList(tiIps);
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "BlockList exists. Updating block list.");
                    var removeIps = currentIps.Where(ip => !tiIps.Contains(ip)).ToList();
                    var addIps = tiIps.Where(ip => !currentIps.Contains(ip)).ToList();
                    await _fireWall.RemoveFromIPBlockList(removeIps);
                    await _fireWall.AddToIPBlockList(addIps);
                }
            }
        }
    }
}