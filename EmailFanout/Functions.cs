using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout
{
    public static class Functions
    {
        /// <summary>
        /// Called by sendgrid when a new email is received
        /// </summary>
        [FunctionName("receive")]
        public static async Task<IActionResult> ReceiveAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log,
            CancellationToken cancellationToken)
        {
            try
            {
                var config = LoadConfig(context.FunctionAppDirectory, log);

                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                log.LogCritical(e, "Failed to process request!");
                return new BadRequestResult();
            }
        }

        /// <summary>
        /// Helper that loads the config values from file, environment variables and keyvault.
        /// </summary>
        private static IConfiguration LoadConfig(string workingDirectory, ILogger log)
        {
            var tokenProvider = new AzureServiceTokenProvider();
            var kvClient = new KeyVaultClient((authority, resource, scope)
                => tokenProvider.KeyVaultTokenCallback(authority, resource, scope));

            var keyVaultName = new ConfigurationBuilder()
            .SetBasePath(workingDirectory)
            .AddJsonFile("local.settings.json", optional: true)
            .AddEnvironmentVariables()
            .Build()["KeyVaultName"];
            if (string.IsNullOrEmpty(keyVaultName))
                throw new NotSupportedException("KeyVaultName is not configured!");

            var builder = new ConfigurationBuilder()
            .AddJsonFile("local.settings.json", optional: true)
            .AddAzureKeyVault($"https://{keyVaultName}.vault.azure.net", kvClient, new DefaultKeyVaultSecretManager())
            .AddEnvironmentVariables();
            return builder.Build();
        }
    }
}
