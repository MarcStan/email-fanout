using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly CloudBlobClient _client;

        public BlobStorageService(CloudStorageAccount storageAccount)
        {
            _client = storageAccount.CreateCloudBlobClient();
        }

        public async Task<string> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            var blob = await GetBlobAsync(containerName, blobName);
            return await blob.DownloadTextAsync(null, null, null, null, cancellationToken);
        }

        public async Task UploadAsync(string containerName, string blobName, byte[] data, CancellationToken cancellationToken)
        {
            var blob = await GetBlobAsync(containerName, blobName);
            await blob.UploadFromByteArrayAsync(data, 0, data.Length, null, null, null, cancellationToken);
        }

        public async Task UploadAsync(string containerName, string blobName, string text, CancellationToken cancellationToken)
        {
            var blob = await GetBlobAsync(containerName, blobName);
            await blob.UploadTextAsync(text, null, null, null, null, cancellationToken);
        }

        private async Task<CloudBlockBlob> GetBlobAsync(string containerName, string blobName)
        {
            var container = _client.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            return container.GetBlockBlobReference(blobName);
        }
    }
}
