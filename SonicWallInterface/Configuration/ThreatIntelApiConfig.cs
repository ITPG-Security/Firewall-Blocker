using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SonicWallInterface.Configuration
{
    public class ThreatIntelApiConfig : IIsPresent
    {
        public string ClientId {get;set;}
        public string TenantId {get;set;}
        public string ClientSecret {get;set;}
        public int? MinConfidence {get;set;}
        [JsonIgnore]
        public bool IsPresent => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(TenantId) && !string.IsNullOrEmpty(ClientSecret);
    }
}
