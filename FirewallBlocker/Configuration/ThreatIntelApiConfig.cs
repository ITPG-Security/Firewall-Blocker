using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FirewallBlocker.Configuration
{
    public class ThreatIntelApiConfig : IIsPresent
    {
        public string? ClientId {get;set;}
        public string? TenantId {get;set;}
        public string? ClientSecret {get;set;}
        public string? WorkspaceId {get;set;}
        public int? MinConfidence {get;set;}
        public string? ExclusionListAlias {get;set;}
        public string? IPv4CollumName {get;set;}
        public int? MaxCount {get;set;}
        [JsonIgnore]
        public bool IsPresent => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(TenantId) && !string.IsNullOrEmpty(ClientSecret);
    }
}
