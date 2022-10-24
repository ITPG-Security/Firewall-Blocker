using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SonicWallInterface.Configuration;
using SonicWallInterface.Helpers;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SonicWallInterface.Services
{
    public class SonicWallTIApi : ISonicWallApi
    {
        private readonly ILogger<SonicWallTIApi> _logger;
        private readonly IOptions<SonicWallConfig> _swCfg;
        private ReaderWriterLock _locker;

        public SonicWallTIApi(ILogger<SonicWallTIApi> logger, IOptions<SonicWallConfig> swCfg)
        {
            _logger = logger;
            _swCfg = swCfg;
            _locker = new ReaderWriterLock();
        }

        private async Task<HttpClient> Login()
        {
            var ctr = 0;
            while (_locker.IsWriterLockHeld && ctr <= 1000)
            {
                await Task.Delay(10);
                ctr++;
            }
            _locker.AcquireWriterLock(100);
            var client = new HttpClient();

            if (!_swCfg.Value.ValidateSSL)
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }
            client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            var loginContent = new StringContent("{\"override\":true}");
            loginContent.Headers.Add("Authorization", $"Basic {StringHelper.EncodeBase64($"{_swCfg.Value.Username}:{_swCfg.Value.Password}")}");
            var responce = await client.PostAsync(_swCfg.Value.FireWallEndpoint + "/auth", loginContent);
            return client;
        }

        private async Task Logout(HttpClient client)
        {
            await client.DeleteAsync(_swCfg.Value.FireWallEndpoint + "/auth");
            _locker.ReleaseWriterLock();
        }

        private async Task CommitChanges(HttpClient client)
        {
            await client.PostAsync(_swCfg.Value.FireWallEndpoint + "/config/pending", new StringContent(""));
        }

        private async Task<bool> DoesIPBlockListExist(HttpClient client)
        {
            var responce = await client.GetAsync(_swCfg.Value.FireWallEndpoint + "/threat/block/ip/");
            return responce.IsSuccessStatusCode;
        }

        private async Task InitiateIPBlockList(HttpClient client, List<string> ips)
        {
            var content = new StringContent(string.Join(Environment.NewLine, ips));
            content.Headers.Add("Content-Type", "application/text");
            var responce = await client.PostAsync(_swCfg.Value.FireWallEndpoint + "/threat/block/ip/", content);
            if (!responce.IsSuccessStatusCode) {
                throw new Exception($"Initiation of block list failed. Error code: \"{responce.StatusCode}\"");
            }
        }

        private async Task UpdateIPBlockList(HttpClient client, List<string> ips)
        {
            var content = new StringContent(string.Join(Environment.NewLine, ips));
            content.Headers.Add("Content-Type", "application/text");
            var responce = await client.PutAsync(_swCfg.Value.FireWallEndpoint + "/threat/block/ip/", content);
            if (!responce.IsSuccessStatusCode)
            {
                throw new Exception($"Initiation of block list failed. Error code: \"{responce.StatusCode}\"");
            }
        }

        public async Task BlockIPsAsync(List<string> ips)
        {
            _logger.Log(LogLevel.Information, "Sarting communication with SonicWall TI API at \"{0}\". Logging in", _swCfg.Value.FireWallEndpoint);
            var client = await Login();
            if(!await DoesIPBlockListExist(client))
            {
                _logger.Log(LogLevel.Information, "BlockList does not exist on \"{0}\". Creating blocklist", _swCfg.Value.FireWallEndpoint);
                await InitiateIPBlockList(client, ips);
            }
            else
            {
                _logger.Log(LogLevel.Information, "BlockList exists on \"{0}\". Updating blocklist", _swCfg.Value.FireWallEndpoint);
                await UpdateIPBlockList(client, ips);
            }
            _logger.Log(LogLevel.Information, "Commiting changes on \"{0}\"", _swCfg.Value.FireWallEndpoint);
            await CommitChanges(client);
            _logger.Log(LogLevel.Information, "Compleated all tasks on \"{0}\". Logging off", _swCfg.Value.FireWallEndpoint);
            await Logout(client);
        }
    }
}
