using Microsoft.Azure.KeyVault;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class KeyVaultHelper : IKeyVaultHelper
    {
        private IKeyVaultClient _keyVaultClient;
        private readonly string _keyVaultUrl;

        public KeyVaultHelper(IKeyVaultClient keyVaultClient, string keyVaultName)
        {
            _keyVaultClient = keyVaultClient;
            _keyVaultUrl = $"https://{keyVaultName}.vault.azure.net";
        }

        public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            var secret = await _keyVaultClient.GetSecretAsync(_keyVaultUrl, secretName, cancellationToken);
            return secret.Value;
        }
    }
}
