using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken);

        Task<HttpResponseMessage> PostAsync(string url, Stream stream, CancellationToken cancellationToken);

        Task<HttpResponseMessage> PostAsync<T>(string url, T json, CancellationToken cancellationToken);
    }
}
