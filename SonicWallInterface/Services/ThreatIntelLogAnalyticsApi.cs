using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Monitor.Query;
using SonicWallInterface.Configuration;
using Azure.Monitor.Query.Models;

namespace SonicWallInterface.Services
{
    public class ThreatIntelLogAnalyticsApi : IThreatIntelApi
    {
        private readonly ILogger<ThreatIntelLogAnalyticsApi> _logger;
        private readonly IOptions<ThreatIntelApiConfig> _tiCfg;
        private LogsQueryClient _logClient;
        private readonly List<string> resources = new List<string>{
            "https://graph.microsoft.com/.default"
        };

        public ThreatIntelLogAnalyticsApi(ILogger<ThreatIntelLogAnalyticsApi> logger, IOptions<ThreatIntelApiConfig> tiCfg){
            _logger = logger;
            _tiCfg = tiCfg;
            Setup();
        }

        private void Setup(){
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            _logClient = new LogsQueryClient(new ClientSecretCredential(_tiCfg.Value.TenantId, _tiCfg.Value.ClientId, _tiCfg.Value.ClientSecret, options));
        }

        public async Task<List<string>> GetCurrentTIIPs(){
            try{
                var response = await _logClient.QueryWorkspaceAsync(
                    _tiCfg.Value.WorkspaceId,
                    "ThreatIntelligenceIndicator" +
                    "| where ExpirationDateTime > now() and " +
                    "ConfidenceScore >= " + _tiCfg.Value.MinConfidence + " and " +
                    "NetworkIP matches regex @\"^(?:[1-2]?[0-9]?[0-9]\\.){3}(?:[1-2]?[0-9]?[0-9])$\" and " +
                    "not(NetworkIP matches regex @\"^(?:192\\.168\\.|10\\.|172\\.(?:1[6-9]|2[0-9]|3[0-1])\\.)\") " +
                    "| summarize by NetworkIP",
                    QueryTimeRange.All
                );
                if(response == null) return new List<string>();
                if(response.Value.Status != LogsQueryResultStatus.Success){
                    _logger.Log(LogLevel.Error, "An error occured during the processing of log querry. {0}", response.Value.Error.Message);
                    throw new Exception(response.Value.Error.Message);
                }
                var ips = response.Value.Table.Rows.Select(r => (string) r["NetworkIP"]).ToList();
                return ips;
            }
            catch(Exception ex){
                _logger.Log(LogLevel.Error, "Request failed: {0}", ex.Message);
                throw ex;
            }
        }
    }
}
