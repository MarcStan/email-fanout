using EmailFanout.Logic.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class ConfigService : IConfigService
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly string _containerName;

        public ConfigService(
            IBlobStorageService blobStorageService,
            IConfiguration configuration)
        {
            _blobStorageService = blobStorageService;
            _containerName = configuration["ConfigContainerName"] ?? throw new KeyNotFoundException("Missing key 'ConfigContainerName'");
        }

        public async Task<EmailConfig> LoadAsync(CancellationToken cancellationToken)
        {
            var data = await _blobStorageService.DownloadAsync(_containerName, "email-fanout.json", cancellationToken);
            var json = JsonConvert.DeserializeObject<EmailConfig>(data);

            return json;
        }
    }
}
