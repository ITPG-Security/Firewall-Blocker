using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FirewallBlocker.Helpers
{
    public static class Events
    {
        public static EventId Read = new EventId(1001, "Read");
        public static EventId Error = new EventId(3001, "Error");
    }
}