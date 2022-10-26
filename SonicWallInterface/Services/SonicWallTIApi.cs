using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SonicWallInterface.Configuration;
using SonicWallInterface.Exceptions;
using SonicWallInterface.Helpers;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SonicWallInterface.Services
{
    public class SonicWallTIApi : ISonicWallApi
    {
        private readonly ILogger<SonicWallTIApi> _logger;
        private readonly IOptions<SonicWallConfig> _swCfg;
        private readonly HttpClientHandler _handler;

        public SonicWallTIApi(ILogger<SonicWallTIApi> logger, IOptions<SonicWallConfig> swCfg)
        {
            _logger = logger;
            _swCfg = swCfg;
            _handler = new HttpClientHandler();
            _handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => 
            {
                return _swCfg.Value.ValidateSSL;
            };
        }

        private string _getAuthValue(){
            return StringHelper.EncodeBase64(_swCfg.Value.Username + ":" + _swCfg.Value.Password);
        }

        public async Task<List<string>> GetIPBlockList()
        {
            var client = new HttpClient(_handler);
            var request = new HttpRequestMessage{
                RequestUri = new Uri(_swCfg.Value.FireWallEndpoint + "/threat/block/ip/"),
                Method = HttpMethod.Get
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _getAuthValue());
            var responce = await client.SendAsync(request);
            if (!responce.IsSuccessStatusCode) return new List<string>();
            return (await responce.Content.ReadAsStringAsync()).Split(Environment.NewLine).ToList();
        }

        public async Task InitiateIPBlockList(List<string> ips)
        {
            _logger.Log(LogLevel.Information, "BlockList does not exist on \"{0}\". Creating blocklist", _swCfg.Value.FireWallEndpoint);
            var client = new HttpClient(_handler);
            var request = new HttpRequestMessage{
                RequestUri = new Uri(_swCfg.Value.FireWallEndpoint + "/threat/block/ip/"),
                Method = HttpMethod.Post,
                Content = new StringContent(string.Join(Environment.NewLine, ips))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _getAuthValue());
            var responce = await client.SendAsync(request);
            if (responce.StatusCode == HttpStatusCode.NotFound){
                throw new UnreachableException($"Endpoint not found at \"{_swCfg.Value.FireWallEndpoint}\". Does the firewall have a valid CFS Licence?");
            }
            if (!responce.IsSuccessStatusCode) {
                throw new Exception($"Initiation of block list failed. Error code: \"{responce.StatusCode}\"");
            }
        }

        public async Task AddToIPBlockList(List<string> ips)
        {
            _logger.Log(LogLevel.Information, "Adding {0} ips to BlockList on \"{1}\".", ips.Count, _swCfg.Value.FireWallEndpoint);
            var client = new HttpClient(_handler);
            var request = new HttpRequestMessage{
                RequestUri = new Uri(_swCfg.Value.FireWallEndpoint + "/threat/block/ip/"),
                Method = HttpMethod.Put,
                Content = new StringContent(string.Join(Environment.NewLine, ips))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _getAuthValue());
            var responce = await client.SendAsync(request);
            if (responce.StatusCode == HttpStatusCode.NotFound){
                throw new UnreachableException($"Endpoint not found at \"{_swCfg.Value.FireWallEndpoint}\". Does the firewall have a valid CFS Licence?");
            }
            if (!responce.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to remove IPs of block list. Error code: \"{responce.StatusCode}\"");
            }
        }

        public async Task RemoveFromIPBlockList(List<string> ips)
        {
            _logger.Log(LogLevel.Information, "Removing {0} ips from BlockList on \"{1}\".", ips.Count, _swCfg.Value.FireWallEndpoint);
            var client = new HttpClient(_handler);
            var request = new HttpRequestMessage{
                RequestUri = new Uri(_swCfg.Value.FireWallEndpoint + "/threat/block/ip/"),
                Method = HttpMethod.Delete,
                Content = new StringContent(string.Join(Environment.NewLine, ips))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _getAuthValue());
            var responce = await client.SendAsync(request);
            if (responce.StatusCode == HttpStatusCode.NotFound){
                throw new UnreachableException($"Endpoint not found at \"{_swCfg.Value.FireWallEndpoint}\". Does the firewall have a valid CFS Licence?");
            }
            if (!responce.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to remove IPs of block list. Error code: \"{responce.StatusCode}\"");
            }
        }
    }
}
