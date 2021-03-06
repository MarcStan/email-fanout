using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using EmailFanout;
using EmailFanout.Logic;
using EmailFanout.Logic.Services;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace EmailFanout
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(p =>
            {
                var connectionString = p.GetRequiredService<IConfiguration>()["AzureWebJobsStorage"];
                return CloudStorageAccount.Parse(connectionString);
            });
            builder.Services.AddSingleton(p =>
            {
                var connectionString = p.GetRequiredService<IConfiguration>()["AzureWebJobsStorage"];
                return Microsoft.Azure.Storage.CloudStorageAccount.Parse(connectionString);
            });

            builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
            builder.Services.AddSingleton(p =>
            {
                var options = new DefaultAzureCredentialOptions();
                var keyVaultName = p.GetRequiredService<IConfiguration>()["KeyVaultName"];
                return new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net"), new DefaultAzureCredential(options));
            });
            builder.Services.AddSingleton<IKeyVaultHelper, KeyVaultHelper>();
            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<IStatusService, StatusService>();
            builder.Services.AddSingleton<IConfigService, ConfigService>();
            builder.Services.AddSingleton<IHttpClient, CustomHttpClient>();
            builder.Services.AddSingleton<ISendgridEmailParser, SendgridEmailParser>();
        }
    }
}
