using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken);
    }

    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PostStreamAsync(this IHttpClient client, string url, Stream stream, CancellationToken cancellationToken)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = new StreamContent(stream);
            return client.SendAsync(req, cancellationToken);
        }

        public static Task<HttpResponseMessage> PostAsync<T>(this IHttpClient client, string url, T json, CancellationToken cancellationToken)
        {
            return PostAsync(client, url, JsonConvert.SerializeObject(json), cancellationToken);
        }

        public static Task<HttpResponseMessage> PostAsync(this IHttpClient client, string url, string json, CancellationToken cancellationToken)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return client.SendAsync(req, cancellationToken);
        }
    }
}
