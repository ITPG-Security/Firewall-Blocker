using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Monitor.Query;
using SonicWallInterface.Configuration;
using Azure.Monitor.Query.Models;
using Moq;
using Azure;

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

        public ThreatIntelLogAnalyticsApi(ILogger<ThreatIntelLogAnalyticsApi> logger, IOptions<ThreatIntelApiConfig> tiCfg, List<string> ips)
        {
            _logger = logger;
            _tiCfg = tiCfg;
            Setup(ips);
        }

        private void Setup(){
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            _logClient = new LogsQueryClient(new ClientSecretCredential(_tiCfg.Value.TenantId, _tiCfg.Value.ClientId, _tiCfg.Value.ClientSecret, options));
        }

        private Response<LogsQueryResult> _getMockResult(List<string> ips){
            var collums = new List<LogsTableColumn>
            {
                MonitorQueryModelFactory.LogsTableColumn("NetworkIP", LogsColumnType.String)
            };
            var rows = new List<LogsTableRow>();
            foreach (var ip in ips)
            {
                rows.Add(MonitorQueryModelFactory.LogsTableRow(collums, new List<string>{
                    ip
                }));
            }
            //Make null JSON objects sdk/monitor/Azure.Monitor.Query/src/Models/MonitorQueryModelFactory.cs https://github.com/Azure/azure-sdk-for-net/pull/26296
            var emptyObject = new {};
            var queryResponse = MonitorQueryModelFactory.LogsQueryResult(
                new List<LogsTable>{MonitorQueryModelFactory.LogsTable("ThreatIntelligenceIndicator", collums, rows)}, 
                new BinaryData(Newtonsoft.Json.JsonConvert.SerializeObject(emptyObject).ToArray().Select(m => ((byte)m)).ToArray()), 
                new BinaryData(Newtonsoft.Json.JsonConvert.SerializeObject(emptyObject).ToArray().Select(m => ((byte)m)).ToArray()),
                new BinaryData(Newtonsoft.Json.JsonConvert.SerializeObject(emptyObject).ToArray().Select(m => ((byte)m)).ToArray()));
            var responceMock = new Mock<Response>();
            responceMock.SetupGet(r => r.Status).Returns(200);
            return Response.FromValue<LogsQueryResult>(queryResponse, responceMock.Object);
        }

        private void Setup(List<string> ips)
        {
            var logMock = new Mock<LogsQueryClient>();
            logMock.Setup(l => l.QueryWorkspaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QueryTimeRange>(), It.IsAny<LogsQueryOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(_getMockResult(ips));
            _logClient = logMock.Object;
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
