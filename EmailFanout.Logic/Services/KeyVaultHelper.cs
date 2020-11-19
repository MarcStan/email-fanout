using Azure.Security.KeyVault.Secrets;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class KeyVaultHelper : IKeyVaultHelper
    {
        private SecretClient _secretClient;

        public KeyVaultHelper(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            var secret = await _secretClient.GetSecretAsync(secretName, null, cancellationToken);
            return secret.Value.Value;
        }
    }
}
