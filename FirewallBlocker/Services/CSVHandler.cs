using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Threading.Tasks;
using FirewallBlocker.Configuration;
using FirewallBlocker.Models;
using Microsoft.Extensions.Options;
using ServiceStack;

namespace FirewallBlocker.Services
{
    public class CSVHandler : IThreatIntelCollector
    {
        private ILogger<CSVHandler> _logger;
        private IOptions<CSVConfig> _csvCfg;
        private HttpClient _client;

        public CSVHandler(ILogger<CSVHandler> logger, IOptions<CSVConfig> csvCfg)
        {
            _logger = logger;
            _csvCfg = csvCfg;
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    if(!_csvCfg.Value.ValidateSSL) return true;
                    
                    _logger.LogDebug($"Start SSL Connection check.");
                    foreach (var c in chain.ChainStatus)
                    {
                        _logger.LogDebug($"ChainStatus={c.Status}");
                    }
                    return SslPolicyErrors.None == sslPolicyErrors;
                }
            };
            _client = new HttpClient(handler);
            if(csvCfg.Value.AuthSchema != null && csvCfg.Value.AuthValue != null)
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(csvCfg.Value.AuthSchema, csvCfg.Value.AuthValue);
            }
        }

        public IEnumerable<string> GetCurrentTI(){
            var response = _client.GetAsync(_csvCfg.Value.URI).Result;
            return _parseResponse(response);
        }

        private IEnumerable<string> _parseResponse(HttpResponseMessage response)
        {
            var list = response.Content.ReadAsStream().ReadToEnd().FromCsv<List<dynamic>>();
            var tiList = new List<CSVIPEntity>();
            if(list != null)
            {
                foreach(dynamic item in list)
                {
                    string ip = Convert.ToString(item[_csvCfg.Value.Schema.First(c => c.CSVType == CSVTypes.IP).Name]);
                    int? score = null;
                    DateTime? dateTime = null;
                    foreach(var collum in _csvCfg.Value.Schema.Where(c => c.CSVType != CSVTypes.IP))
                    {
                        switch(collum.CSVType)
                        {
                            case CSVTypes.IP:
                                ip = Convert.ToString(item[collum.Name]);
                                break;
                            case CSVTypes.SCORE:
                                score = Convert.ToInt32(item[collum.Name]);
                                break;
                            case CSVTypes.TIME:
                                dateTime = Convert.ToDateTime(item[collum.Name]);
                                break;
                        }
                    }
                    tiList.Add(new CSVIPEntity(ip, score, dateTime));
                }
            }
            var SortKey = string.Join("|", _csvCfg.Value.SortBy.Distinct().Select(i => i.ToString()));
            switch (SortKey)
            {
                case "IP|SCORE|TIME":
                    return tiList
                        .OrderBy(s => s.IP)
                        .ThenByDescending(s => s.Score)
                        .ThenByDescending(s => s.DateTime)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "IP|TIME|SCORE":
                    return tiList
                        .OrderBy(s => s.IP)
                        .ThenByDescending(s => s.DateTime)
                        .ThenByDescending(s => s.Score)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "SCORE|IP|TIME":
                    return tiList
                        .OrderByDescending(s => s.Score)
                        .ThenBy(s => s.IP)
                        .ThenByDescending(s => s.DateTime)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "SCORE|TIME|IP":
                    return tiList
                        .OrderByDescending(s => s.Score)
                        .ThenByDescending(s => s.DateTime)
                        .ThenBy(s => s.IP)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "TIME|IP|SCORE":
                    return tiList
                        .OrderByDescending(s => s.DateTime)
                        .ThenBy(s => s.IP)
                        .ThenByDescending(s => s.Score)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "TIME|SCORE|IP":
                    return tiList
                        .OrderByDescending(s => s.DateTime)
                        .ThenByDescending(s => s.Score)
                        .ThenBy(s => s.IP)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "IP|SCORE":
                    return tiList
                        .OrderBy(s => s.IP)
                        .ThenByDescending(s => s.Score)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "IP|TIME":
                    return tiList
                        .OrderBy(s => s.IP)
                        .ThenByDescending(s => s.DateTime)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "SCORE|IP":
                    return tiList
                        .OrderByDescending(s => s.Score)
                        .ThenBy(s => s.IP)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "SCORE|TIME":
                    return tiList
                        .OrderByDescending(s => s.Score)
                        .ThenByDescending(s => s.DateTime)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "TIME|IP":
                    return tiList
                        .OrderByDescending(s => s.DateTime)
                        .ThenBy(s => s.IP)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "TIME|SCORE":
                    return tiList
                        .OrderByDescending(s => s.DateTime)
                        .ThenByDescending(s => s.Score)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "IP":
                    return tiList
                        .OrderBy(s => s.IP)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "SCORE":
                    return tiList
                        .OrderByDescending(s => s.Score)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
                case "TIME":
                    return tiList
                        .OrderByDescending(s => s.DateTime)
                        .Take(_csvCfg.Value.MaxCount)
                        .Select(i => i.IP);
            }
            return tiList.Take(_csvCfg.Value.MaxCount).Select(i => i.IP);
        }
    }
}