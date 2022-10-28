using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SonicWallInterface.Configuration
{
    public class SonicWallConfig : IIsPresent
    {
        public string FireWallEndpoint { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool ValidateSSL { get; set; }

        [JsonIgnore]
        public bool IsPresent => !string.IsNullOrEmpty(FireWallEndpoint) && !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
    }
}
