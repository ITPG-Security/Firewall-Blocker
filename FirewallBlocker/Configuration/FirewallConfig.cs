using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FirewallBlocker.Configuration
{
    public class FirewallConfig : IIsPresent
    {

        public List<SonicWallConfig>? SonicWalls { get; set; }

        [JsonIgnore]
        public bool IsPresent => SonicWalls != null && SonicWalls.Count > 0;
    }
}