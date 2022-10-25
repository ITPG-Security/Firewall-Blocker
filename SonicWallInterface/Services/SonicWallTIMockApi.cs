using Microsoft.Extensions.Logging;

namespace SonicWallInterface.Services
{
    public class SonicWallTIMockApi : ISonicWallApi
    {
        private readonly ILogger<SonicWallTIMockApi> _logger;
        private readonly IThreatIntelApi _threat;
        private readonly ReaderWriterLock _locker = new ReaderWriterLock();
        private List<string> _ipAddresses;

        public SonicWallTIMockApi(ILogger<SonicWallTIMockApi> logger, IThreatIntelApi threat)
        {
            _logger = logger;
            _threat = threat;
            _ipAddresses = new List<string>();
        }

        public async Task BlockIPsAsync()
        {
            _locker.AcquireWriterLock(10);
            var ips = await _threat.GetCurrentTIIPs();
            _ipAddresses = ips.ToList();
            _locker.ReleaseWriterLock();
        }
    }
}
