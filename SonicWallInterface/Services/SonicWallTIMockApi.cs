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

        public List<string> IpAddresses => _ipAddresses;

        public Task AddToIPBlockList(List<string> ips)
        {
            _ipAddresses.AddRange(ips.Where(ip => !_ipAddresses.Contains(ip)));
            return Task.CompletedTask;
        }

        public Task<List<string>> GetIPBlockList()
        {
            return Task.Factory.StartNew<List<string>>(() => {
                return _ipAddresses.ToList();
            });
        }

        public Task InitiateIPBlockList(List<string> ips)
        {
            _ipAddresses = ips.ToList();
            return Task.CompletedTask;
        }

        public Task RemoveFromIPBlockList(List<string> ips)
        {
            _ipAddresses = _ipAddresses.Where(ip => !ips.Contains(ip)).ToList();
            return Task.CompletedTask;
        }
    }
}
