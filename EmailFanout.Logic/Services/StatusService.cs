using EmailFanout.Logic.Config;
using EmailFanout.Logic.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class StatusService : IStatusService
    {
        private readonly CloudTable _table;

        public StatusService(
            CloudStorageAccount storageAccount,
            IConfiguration configuration)
        {
            var client = storageAccount.CreateCloudTableClient();
            _table = client.GetTableReference(configuration["EmailStatusTableName"] ?? throw new KeyNotFoundException("Missing key 'EmailStatusTableName'"));
        }

        public async Task<IReadOnlyDictionary<string, StatusModel>> GetStatiAsync(EmailRequest request, CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(null, null, cancellationToken);
            var partitionMatch = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, StatusModel.GetPartitionKey(request));
            var tableQuery = new TableQuery<StatusModel>
            {
                FilterString = partitionMatch
            };

            TableContinuationToken token = null;
            var stati = new List<StatusModel>();
            do
            {
                var result = await _table.ExecuteQuerySegmentedAsync(tableQuery, null, null, null, cancellationToken);
                token = result.ContinuationToken;
                stati.AddRange(result.Results);
            }
            while (token != null);
            return stati.ToDictionary(x => x.ActionId, x => x);
        }

        public Task<StatusModel> UpdateAsync(EmailRequest request, EmailAction action, EmailFanoutStatus status, CancellationToken cancellationToken)
            => UpdateAsync(request, action, status, false, cancellationToken);
        public async Task<StatusModel> UpdateAsync(EmailRequest request, EmailAction action, EmailFanoutStatus status, bool @override, CancellationToken cancellationToken)
        {
            await _table.CreateIfNotExistsAsync(null, null, cancellationToken);
            var model = new StatusModel(request, action, status);
            await _table.ExecuteAsync(@override ? TableOperation.InsertOrReplace(model) : TableOperation.Insert(model), null, null, cancellationToken);
            return model;
        }
    }
}
