using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SonicWallInterface.Configuration;
using SonicWallInterface.Helpers;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SonicWallInterface.Services
{
    public class SonicWallTIMockApi : ISonicWallApi
    {
        private readonly ILogger<SonicWallTIMockApi> _logger;
        private readonly ReaderWriterLock _locker = new ReaderWriterLock();
        private List<string> _ipAddresses;

        public SonicWallTIMockApi(ILogger<SonicWallTIMockApi> logger)
        {
            _logger = logger;
            _ipAddresses = new List<string>();
        }

        public Task BlockIPsAsync(List<string> ips)
        {
            _locker.AcquireWriterLock(10);
            _ipAddresses = _ipAddresses.Where(ip => ips.Contains(ip)).ToList();
            _locker.ReleaseWriterLock();
            return Task.CompletedTask;
        }
    }
}
