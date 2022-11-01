using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SonicWallInterface.Configuration
{
    public class AppConfig : IIsPresent
    {
        public string SiteName { get; set; }

        [JsonIgnore]
        public bool IsPresent => !string.IsNullOrEmpty(SiteName);
    }
}
