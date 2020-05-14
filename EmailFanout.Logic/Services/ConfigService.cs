using EmailFanout.Logic.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class ConfigService : IConfigService
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly string _containerName;
        private const string _configFile = "email-fanout.json";

        public ConfigService(
            IBlobStorageService blobStorageService,
            IConfiguration configuration)
        {
            _blobStorageService = blobStorageService;
            _containerName = configuration["ConfigContainerName"] ?? throw new KeyNotFoundException("Missing key 'ConfigContainerName'");
        }

        public async Task<EmailConfig> LoadAsync(CancellationToken cancellationToken)
        {
            var data = await _blobStorageService.DownloadAsync(_containerName, _configFile, cancellationToken);
            try
            {
                var json = JsonConvert.DeserializeObject<EmailConfig>(data);
                var uniqueIds = json.Rules
                    .SelectMany(r => r.Actions.Select(a => a.Id))
                    .ToList();
                var duplicates = uniqueIds
                    .GroupBy(x => x)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                if (duplicates.Any())
                {
                    throw new NotSupportedException($"Duplicate keys '{string.Join(", ", duplicates)}' where found in config. Each action must have a unique key!");
                }
                return json;
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Failed to parse config file {_configFile} in container {_containerName}.", ex);
            }
        }
    }
}
