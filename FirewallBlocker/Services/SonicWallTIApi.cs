using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FirewallBlocker.Configuration;
using FirewallBlocker.Exceptions;
using FirewallBlocker.Helpers;
using System.Net;
using System.Net.Http.Headers;
using Moq.Protected;
using Moq;
using System.Net.Security;

namespace FirewallBlocker.Services
{
    public class SonicWallTIApi : IFireWallApi
    {
        private readonly ILogger<SonicWallTIApi> _logger;
        private readonly SonicWallConfig _swCfg;
        private readonly HttpClientHandler _handler;
        private readonly bool _isMock;
        private List<string> _mockIps;

        public SonicWallTIApi(ILogger<SonicWallTIApi> logger, SonicWallConfig swCfg)
        {
            _logger = logger;
            _swCfg = swCfg;
            _handler = new HttpClientHandler{
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => 
                {
                    if(!_swCfg.ValidateSSL) return true;
                    _logger.LogDebug($"Start SSL Connection check.");
                    foreach (var c in chain.ChainStatus)
                    {
                        _logger.LogDebug($"ChainStatus={c.Status}");
                    }
                    return SslPolicyErrors.None == sslPolicyErrors;
                }
            };
            _isMock = false;
            _mockIps = new List<string>();
        }

        public SonicWallTIApi(ILogger<SonicWallTIApi> logger, SonicWallConfig swCfg, List<string> ips)
        {
            _logger = logger;
            _swCfg = swCfg;
            _isMock = true;
            _mockIps = ips;
        }

        private string _getAuthValue(){
            return StringHelper.EncodeBase64(_swCfg.Username + ":" + _swCfg.Password);
        }

        private HttpRequestMessage _getIPBlockListRequest()
        {
            var request = new HttpRequestMessage{
                RequestUri = new Uri(_swCfg.FireWallEndpoint + "/threat/block/ip/"),
                Method = HttpMethod.Get
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _getAuthValue());
            return request;
        }

        private HttpResponseMessage _getIPBlockListResponse()
        {
            return new HttpResponseMessage{
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Join("\n", _mockIps))
            };
        }

        private HttpRequestMessage _postIPBlockListRequest(List<string> ips)
        {
            var request = new HttpRequestMessage{
                RequestUri = new Uri(_swCfg.FireWallEndpoint + "/threat/block/ip/"),
                Method = HttpMethod.Post,
                Content = new StringContent(string.Join("\n", ips))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _getAuthValue());
            _mockIps = ips.ToList();
            return request;
        }

        private HttpResponseMessage _postIPBlockListResponse()
        {
            return new HttpResponseMessage{
                StatusCode = HttpStatusCode.Created
            };
        }

        private HttpRequestMessage _putIPBlockListRequest(List<string> ips)
        {
            var request = new HttpRequestMessage{
                RequestUri = new Uri(_swCfg.FireWallEndpoint + "/threat/block/ip/"),
                Method = HttpMethod.Put,
                Content = new StringContent(string.Join("\n", ips))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _getAuthValue());
            _mockIps.AddRange(ips);
            return request;
        }

        private HttpResponseMessage _putIPBlockListResponse()
        {
            return new HttpResponseMessage{
                StatusCode = HttpStatusCode.NoContent
            };
        }

        private HttpRequestMessage _deleteIPBlockListRequest(List<string> ips)
        {
            var request = new HttpRequestMessage{
                RequestUri = new Uri(_swCfg.FireWallEndpoint + "/threat/block/ip/"),
                Method = HttpMethod.Delete,
                Content = new StringContent(string.Join("\n", ips))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _getAuthValue());
            _mockIps = _mockIps.Where(ip => !ips.Contains(ip)).ToList();
            return request;
        }

        private HttpResponseMessage _deleteIPBlockListResponse()
        {
            return new HttpResponseMessage{
                StatusCode = HttpStatusCode.NoContent
            };
        }

        private void _handlePost(List<string> ips)
        {
            _mockIps = ips.ToList();
        }

        private void _handlePut(List<string> ips)
        {
            _mockIps.AddRange(ips.Where(ip => !_mockIps.Contains(ip)));
        }

        private void _handleDelete(List<string> ips)
        {
            _mockIps = _mockIps.Where(ip => !ips.Contains(ip)).ToList();
        }
        
        private HttpMessageHandler _getMockHandler()
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
                ).ReturnsAsync(_getIPBlockListResponse());
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
                ).ReturnsAsync(_postIPBlockListResponse());
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>()
                ).ReturnsAsync(_putIPBlockListResponse());
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>()
                ).ReturnsAsync(_deleteIPBlockListResponse());
            return mockHandler.Object;
        }

        public async Task<List<string>> GetIPBlockList()
        {
            HttpClient client;
            if(_isMock)
            {
                client = new HttpClient(_getMockHandler());
            }
            else
            {
                client = new HttpClient(_handler);
                client.Timeout = TimeSpan.FromSeconds(10);
            }
            var request = _getIPBlockListRequest();
            var responce = await client.SendAsync(request);
            if (!responce.IsSuccessStatusCode) return new List<string>();
            var result = (await responce.Content.ReadAsStringAsync()).Split("\n").Where(ip => !string.IsNullOrEmpty(ip)).ToList();
            return result;
        }

        public async Task InitiateIPBlockList(List<string> ips)
        {
            if(ips.Count <= 0){
                _logger.Log(LogLevel.Debug, "A POST request was made. However, an empty list was used.");
                return;
            }
            _logger.Log(LogLevel.Debug, "BlockList does not exist on \"{0}\". Creating blocklist", _swCfg.FireWallEndpoint);
            HttpClient client;
            if(_isMock)
            {
                client = new HttpClient(_getMockHandler());
            }
            else
            {
                client = new HttpClient(_handler);
                client.Timeout = TimeSpan.FromMinutes(1);
            }
            var request = _postIPBlockListRequest(ips);
            var responce = await client.SendAsync(request);
            if (responce.StatusCode == HttpStatusCode.NotFound){
                throw new UnreachableException($"Endpoint not found at \"{_swCfg.FireWallEndpoint}\". Does the firewall have a valid CFS Licence?");
            }
            if (!responce.IsSuccessStatusCode) {
                throw new Exception($"Initiation of block list failed. Error code: \"{responce.StatusCode}\"");
            }
            if(_isMock) _handlePost(ips);
        }

        public async Task AddToIPBlockList(List<string> ips)
        {
            if(ips.Count <= 0){
                _logger.Log(LogLevel.Debug, "A PUT request was made. However, an empty list was used.");
                return;
            }
            _logger.Log(LogLevel.Debug, "Adding {0} ips to BlockList on \"{1}\".", ips.Count, _swCfg.FireWallEndpoint);
            HttpClient client;
            if(_isMock)
            {
                client = new HttpClient(_getMockHandler());
            }
            else
            {
                client = new HttpClient(_handler);
                client.Timeout = TimeSpan.FromMinutes(1);
            }
            var request = _putIPBlockListRequest(ips);
            var responce = await client.SendAsync(request);
            if (responce.StatusCode == HttpStatusCode.NotFound){
                throw new UnreachableException($"Endpoint not found at \"{_swCfg.FireWallEndpoint}\". Does the firewall have a valid CFS Licence?");
            }
            if (!responce.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to remove IPs of block list. Error code: \"{responce.StatusCode}\"");
            }
            if(_isMock) _handlePut(ips);
        }

        public async Task RemoveFromIPBlockList(List<string> ips)
        {
            if(ips.Count <= 0){
                _logger.Log(LogLevel.Debug, "A DELETE request was made. However, an empty list was used.");
                return;
            }
            _logger.Log(LogLevel.Debug, "Removing {0} ips from BlockList on \"{1}\".", ips.Count, _swCfg.FireWallEndpoint);
            HttpClient client;
            if(_isMock)
            {
                client = new HttpClient(_getMockHandler());
            }
            else
            {
                client = new HttpClient(_handler);
                client.Timeout = TimeSpan.FromMinutes(2);
            }
            var request = _deleteIPBlockListRequest(ips);
            var responce = await client.SendAsync(request);
            if (responce.StatusCode == HttpStatusCode.NotFound){
                throw new UnreachableException($"Endpoint not found at \"{_swCfg.FireWallEndpoint}\". Does the firewall have a valid CFS Licence?");
            }
            if (!responce.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to remove IPs of block list. Error code: \"{responce.StatusCode}\"");
            }
            if(_isMock) _handleDelete(ips);
        }
    }
}
