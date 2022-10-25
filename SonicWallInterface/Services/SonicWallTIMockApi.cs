using Microsoft.Extensions.Logging;

namespace SonicWallInterface.Services
{
    public class SonicWallTIMockApi : ISonicWallApi
    {
        private readonly ILogger<SonicWallTIMockApi> _logger;
        private readonly IThreatIntelApi _threat;
        private List<string> _ipAddresses;

        public SonicWallTIMockApi(ILogger<SonicWallTIMockApi> logger, IThreatIntelApi threat, List<string> ips)
        {
            _logger = logger;
            _threat = threat;
            _ipAddresses = ips.ToList();
        }

        public async Task BlockIPsAsync()
        {
            _ipAddresses = (await _threat.GetCurrentTIIPs()).ToList();
        }

        public List<string> GetIps()
        {
            return _ipAddresses.ToList();
        }
    }
}
