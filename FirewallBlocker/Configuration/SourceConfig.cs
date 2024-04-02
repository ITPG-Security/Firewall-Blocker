using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FirewallBlocker.Configuration
{
    public class SourceConfig : IIsPresent
    {
        public ThreatIntelApiConfig? ThreatIntelApiConfig { get; set; }
        public CSVConfig? CSVConfig { get; set; }

        [JsonIgnore]
        public bool IsPresent => 
            (
                ThreatIntelApiConfig != null && 
                ThreatIntelApiConfig.IsPresent && 
                (CSVConfig == null || !CSVConfig.IsPresent)
            ) ||
            (
                CSVConfig != null && 
                CSVConfig.IsPresent && 
                (ThreatIntelApiConfig == null || !ThreatIntelApiConfig.IsPresent)
            ) ;
    }
}