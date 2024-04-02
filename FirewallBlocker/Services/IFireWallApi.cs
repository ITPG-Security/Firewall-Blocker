using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirewallBlocker.Services
{
    public interface IFireWallApi
    {
        Task<List<string>> GetIPBlockList();
        Task InitiateIPBlockList(List<string> ips);
        Task AddToIPBlockList(List<string> ips);
        Task RemoveFromIPBlockList(List<string> ips);
    }
}
