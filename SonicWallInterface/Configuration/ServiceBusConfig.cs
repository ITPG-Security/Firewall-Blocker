using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SonicWallInterface.Configuration
{
    public class ServiceBusConfig : IIsPresent
    {
        public string ConnectionString { get; set; }

        [JsonIgnore]
        public bool IsPresent => !string.IsNullOrEmpty(ConnectionString);

        public override string ToString()
        {
            return $"ConnectionString={(IsPresent?"******":"NULL")}{Environment.NewLine}";
        }
    }
}
