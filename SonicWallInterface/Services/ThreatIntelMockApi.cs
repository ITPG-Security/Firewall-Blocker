using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Beta;
using SonicWallInterface.Configuration;

namespace SonicWallInterface.Services
{
    public class ThreatIntelMockApi : IThreatIntelApi
    {
        private List<string> _ipAddresses;
        private readonly ILogger<ThreatIntelMockApi> _logger;
        private readonly ReaderWriterLock _locker = new ReaderWriterLock();

        public ThreatIntelMockApi(ILogger<ThreatIntelMockApi> logger){
            _logger = logger;
            _ipAddresses = new List<string>();
        }
        public ThreatIntelMockApi(ILogger<ThreatIntelMockApi> logger, List<string> ipAddresses){
            _logger = logger;
            _ipAddresses = ipAddresses.ToList();
        }

        public Task<List<string>> GetCurrentTIIPs(){
            return Task<List<string>>.Factory.StartNew(() => {
                _locker.AcquireReaderLock(10);
                var tmp = _ipAddresses.ToList();
                _locker.ReleaseReaderLock();
                return tmp;
            });
        }

        public Task ResetIPs(List<string> ips){
            _locker.AcquireWriterLock(10);
            _ipAddresses = ips.ToList();
            _locker.ReleaseWriterLock();
            return Task.CompletedTask;
        }
    }
}
