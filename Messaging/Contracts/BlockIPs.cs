using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messaging.Contracts
{
    public record BlockIPs
    {
        public DateTime DateTime { get; init; } 
        public string CreatedBy { get; init; }
    }
}
