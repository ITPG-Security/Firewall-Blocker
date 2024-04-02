using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirewallBlocker.Models
{
    public class CSVIPEntity
    {
        public string IP { get; set; }
        public int? Score { get; set; }
        public DateTime? DateTime { get; set; }

        public CSVIPEntity(string ip, int? score = null, DateTime? dateTime = null)
        {
            IP = ip;
            Score = score;
            DateTime = dateTime;
        }
    }
}