using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class CustomHttpClient : IHttpClient
    {
        private readonly HttpClient _client = new HttpClient();

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
            => _client.SendAsync(message, cancellationToken);
    }
}
