using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirewallBlocker.Services
{
    public interface IThreatIntelCollector
    {
        public IEnumerable<string> GetCurrentTI();
    }
}