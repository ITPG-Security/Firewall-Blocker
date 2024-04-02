using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirewallBlocker.Services
{
    public class HttpIPListApi : IHttpIPListApi
    {
        private readonly ILogger<HttpIPListApi> _logger;
        private List<string> _ips;
        private ReaderWriterLock _rwl;

        public HttpIPListApi(ILogger<HttpIPListApi> logger)
        {
            _logger = logger;
            _ips = new List<string>();
            _rwl = new ReaderWriterLock();
        }

        public HttpIPListApi(ILogger<HttpIPListApi> logger, List<string> ips)
        {
            _logger = logger;
            _ips = ips;
            _rwl = new ReaderWriterLock();
        }

        public string GetIPBlockList()
        {
            _logger.LogDebug("Received Get request");
            try
            {
                _rwl.AcquireReaderLock(100);
                _logger.LogDebug("Acquired reader lock @ {}", DateTime.UtcNow.ToString());
                string list = "";
                try
                {
                    list = String.Join(Environment.NewLine, _ips.ToArray());
                }
                finally
                {
                    _rwl.ReleaseReaderLock();
                    _logger.LogDebug("Released reader lock @ {}", DateTime.UtcNow.ToString());
                }
                return list;
            }
            catch (ApplicationException e)
            {
                _logger.LogError("Read request timed out!"); 
                throw e;
            }
        }

        public void OverwriteIPBlockList(IEnumerable<string> ips)
        {
            try
            {
                _rwl.AcquireWriterLock(100);
                _logger.LogDebug("Acquired Writer lock @ {}", DateTime.UtcNow.ToString());
                var list = new List<string>();
                list.AddRange(ips);
                try
                {
                    _ips = list;
                }
                finally
                {
                    _rwl.ReleaseWriterLock();
                    _logger.LogDebug("Released Writer lock @ {}", DateTime.UtcNow.ToString());
                }
            }
            catch (ApplicationException e)
            {
                throw e;
            }
        }

    }
}