using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FirewallBlocker.Configuration;

namespace FirewallBlocker.Services
{
    public class TIHandler : ITIHandler
    {
        private readonly ILogger<TIHandler> _logger;
        private readonly ILoggerFactory _logFactory;
        private readonly List<IFireWallApi> _fireWalls;
        private readonly IOptions<FirewallConfig> _fwConf;
        private readonly IThreatIntelCollector _threat;
        private readonly IHttpIPListApi _httpIps;

        public TIHandler(ILogger<TIHandler> logger, ILoggerFactory logFactory, IThreatIntelCollector threat, IHttpIPListApi httpIps, IOptions<FirewallConfig> fwConf)
        {
            _logger = logger;
            _logFactory = logFactory;
            _fwConf = fwConf;
            _fireWalls = new List<IFireWallApi>();
            if (_fwConf.Value.SonicWalls != null)
            {
                foreach (var swConf in _fwConf.Value.SonicWalls)
                {
                    _fireWalls.Add(new SonicWallTIApi(_logFactory.CreateLogger<SonicWallTIApi>(), swConf));
                }
            }
            _threat = threat;
            _httpIps = httpIps;
        }
        public TIHandler(ILogger<TIHandler> logger, IThreatIntelCollector threat, IHttpIPListApi httpIps, List<IFireWallApi> firewalls)
        {
            _logger = logger;
            _fireWalls = firewalls;
            _threat = threat;
            _httpIps = httpIps;
        }

        public async Task HandleTI()
        {
            _logger.Log(LogLevel.Debug, "Gathering TI from Sentinel");
            var tiIps = _threat.GetCurrentTI();
            _httpIps.OverwriteIPBlockList(tiIps);
            var firewallTasks = new List<Task>();
            foreach (var firewall in _fireWalls)
            {
                firewallTasks.Add(HandleFirewall(tiIps, firewall));
            }
            while (firewallTasks.Any(f => !f.IsCompleted))
            {
                await Task.Delay(100);
            }
        }

        private async Task HandleFirewall(IEnumerable<string> tiIps, IFireWallApi firewall)
        {
            var currentIps = await firewall.GetIPBlockList();
            if (currentIps.Count <= 0)
            {
                _logger.Log(LogLevel.Debug, "Initializing block list.");
                await firewall.InitiateIPBlockList(tiIps.ToList());
            }
            else
            {
                _logger.Log(LogLevel.Debug, "BlockList exists. Updating block list.");
                var removeIps = currentIps.Where(ip => !tiIps.Contains(ip)).ToList();
                var addIps = tiIps.Where(ip => !currentIps.Contains(ip)).ToList();
                await firewall.RemoveFromIPBlockList(removeIps);
                await firewall.AddToIPBlockList(addIps);
            }
        }
    }
}