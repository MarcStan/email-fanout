using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IBlobStorageService
    {
        Task UploadAsync(string containerName, string blobName, string text, CancellationToken cancellationToken);

        Task UploadAsync(string containerName, string blobName, byte[] data, CancellationToken cancellationToken);

        Task<string> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken);
    }
}
