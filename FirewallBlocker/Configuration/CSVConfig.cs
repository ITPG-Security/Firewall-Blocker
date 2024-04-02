using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FirewallBlocker.Configuration
{
    public class CSVConfig : IIsPresent
    {
        public string? URI { get; set; }
        public string? AuthValue { get; set; }
        public string AuthSchema { get; set; } = "Bearer";
        public bool ValidateSSL { get; set; } = true;
        public int MaxCount { get; set; } = 100;
        public List<CSVTypes> SortBy { get; set; } = new List<CSVTypes>();
        public List<CSVSchema> Schema { get; set; } = new List<CSVSchema>
        {
            new CSVSchema {
                Name = "IP",
                CSVType = CSVTypes.IP
            }
        };
        

        [JsonIgnore]
        public bool IsPresent => !string.IsNullOrEmpty(URI) && Schema.Count > 0;

        public override string ToString()
        {
            return $"URI={URI};AuthSchema={(string.IsNullOrEmpty(AuthSchema) ? "NULL" : AuthSchema)};AuthValue={(string.IsNullOrEmpty(AuthValue) ? "NULL" : "********")}";
        }
    }

    public class CSVSchema
    {
        public string? Name { get; set;}
        public CSVTypes? CSVType { get; set; }
    }

    public enum CSVTypes
    {
        IP,
        SCORE,
        TIME
    }
}