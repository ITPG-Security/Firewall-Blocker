using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonicWallInterface.Services
{
    public interface IHttpIPListApi
    {
        
        public string GetIPBlockList();

        public void OverwriteIPBlockList(List<string> ips);
    }
}