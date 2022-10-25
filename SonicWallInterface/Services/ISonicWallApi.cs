using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicWallInterface.Services
{
    public interface ISonicWallApi
    {
        Task BlockIPsAsync();
    }
}
