using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Beta;
using SonicWallInterface.Configuration;
using Microsoft.Graph.Beta.Security.TiIndicators;
using SonicWallInterface.Helpers;

namespace SonicWallInterface.Services
{
    public class ThreatIntelApi : IThreatIntelApi
    {
        private readonly ILogger<ThreatIntelApi> _logger;
        private readonly IOptions<ThreatIntelApiConfig> _tiCfg;
        private readonly string _baseUrl = "https://graph.microsoft.com/beta/security/tiIndicators";
        private GraphServiceClient _graphClient;
        private readonly List<string> scopes = new List<string>{
            "https://graph.microsoft.com/.default"
        };

        public ThreatIntelApi(ILogger<ThreatIntelApi> logger, IOptions<ThreatIntelApiConfig> tiCfg){
            throw new NotImplementedException("MS Graph is currently not supported.");
            _logger = logger;
            _tiCfg = tiCfg;
            Setup();
        }

        private void Setup(){
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            _graphClient = new GraphServiceClient(new ClientSecretCredential(_tiCfg.Value.TenantId, _tiCfg.Value.ClientId, _tiCfg.Value.ClientSecret, options), scopes);
        }

        public async Task<List<string>> GetCurrentTIIPs(){
            try{
                var tmp = await _graphClient.Security.TiIndicators.GetAsync(requestConfiguration => {
                    requestConfiguration.QueryParameters.Filter = $"expirationDateTime ge {DateTime.UtcNow} and " +
                    $"confidence ge {(_tiCfg.Value.MinConfidence != null ? _tiCfg.Value.MinConfidence : 50)}";
                    requestConfiguration.QueryParameters.Select = new string[]{
                        "networkIPv4"
                    };
                });
                return tmp.Value.Select(ti => ti.NetworkIPv4).ToList();
            }
            catch(Exception ex){
                _logger.Log(LogLevel.Error, Events.Error, "Request failed: {0}", ex.Message);
                throw ex;
            }
        }
    }
}
