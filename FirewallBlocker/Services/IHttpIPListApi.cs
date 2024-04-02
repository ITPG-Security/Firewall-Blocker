using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirewallBlocker.Services
{
    public interface IHttpIPListApi
    {
        
        public string GetIPBlockList();

        public void OverwriteIPBlockList(IEnumerable<string> ips);
    }
}