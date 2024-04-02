using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Monitor.Query;
using FirewallBlocker.Configuration;
using Azure.Monitor.Query.Models;
using Moq;
using Azure;
using FirewallBlocker.Helpers;

namespace FirewallBlocker.Services
{
    public class ThreatIntelLogAnalyticsApi : IThreatIntelCollector
    {
        private readonly ILogger<ThreatIntelLogAnalyticsApi> _logger;
        private readonly IOptions<ThreatIntelApiConfig> _tiCfg;
        private LogsQueryClient _logClient;

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

        private string _getTiQuery()
        {
            return 
                "ThreatIntelligenceIndicator" +
                "| where ExpirationDateTime > now() and " +
                "ConfidenceScore >= " + _tiCfg.Value.MinConfidence + " and " +
                "not(ipv4_is_private( NetworkIP)) " +
                "| sort by ConfidenceScore desc, TimeGenerated desc " +
                "| summarize by NetworkIP" +
                "| take " + (_tiCfg.Value.MaxCount != null ? _tiCfg.Value.MaxCount : 1000).ToString();
        }

        private string _getTiQueryWithExclusion()
        {
            if(string.IsNullOrEmpty(_tiCfg.Value.ExclusionListAlias) || string.IsNullOrEmpty(_tiCfg.Value.IPv4CollumName)) throw new NullReferenceException("Invalid TI configuration found.");
            return 
                "let exclusions = union isfuzzy=true (datatable(IPv4:string)[]), (_GetWatchlist(\"" + _tiCfg.Value.ExclusionListAlias + "\")| project " + _tiCfg.Value.IPv4CollumName + "); " +
                "ThreatIntelligenceIndicator " +
                "| summarize arg_max(TimeGenerated, *) by IndicatorId " +
                "| where ExpirationDateTime > now() and " +
                "ConfidenceScore >= " + _tiCfg.Value.MinConfidence + " and " +
                "NetworkIP !in~ (exclusions) and " +
                "not(ipv4_is_private( NetworkIP)) " +
                "| sort by ConfidenceScore desc, TimeGenerated desc " +
                "| project NetworkIP " +
                "| take " + (_tiCfg.Value.MaxCount != null ? _tiCfg.Value.MaxCount : 1000).ToString();
        }

        public IEnumerable<string> GetCurrentTI(){
            string query = (string.IsNullOrEmpty(_tiCfg.Value.ExclusionListAlias) || string.IsNullOrEmpty(_tiCfg.Value.IPv4CollumName)) ? _getTiQuery() : _getTiQueryWithExclusion();
            var response = _logClient.QueryWorkspaceAsync(
                _tiCfg.Value.WorkspaceId,
                query,
                QueryTimeRange.All
            ).Result;
            if(response == null) return new List<string>();
            if(response.Value.Status != LogsQueryResultStatus.Success){
                throw new Exception(response.Value.Error.Message);
            }
            var ips = response.Value.Table.Rows.Select(r => (string) r["NetworkIP"]).ToList();
            return ips;
        }
    }
}
