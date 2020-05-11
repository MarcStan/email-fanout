using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class CustomHttpClient : IHttpClient
    {
        private readonly HttpClient _client = new HttpClient();

        public Task<HttpResponseMessage> PostAsync(string url, Stream stream, CancellationToken cancellationToken)
            => _client.PostAsync(url, new StreamContent(stream), cancellationToken);

        public Task<HttpResponseMessage> PostAsync<T>(string url, T json, CancellationToken cancellationToken)
            => _client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json"), cancellationToken);
    }
}
