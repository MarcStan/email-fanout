using EmailFanout;
using EmailFanout.Logic;
using EmailFanout.Logic.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;

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

            builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
            builder.Services.AddSingleton<IKeyVaultHelper>(p =>
            {
                var keyVaultName = p.GetRequiredService<IConfiguration>()["KeyVaultName"];
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback));
                return new KeyVaultHelper(keyVaultClient, keyVaultName);
            });
            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<IStatusService, StatusService>();
            builder.Services.AddSingleton<IConfigService, ConfigService>();
            builder.Services.AddSingleton<IHttpClient, CustomHttpClient>();
            builder.Services.AddSingleton<ISendgridEmailParser, SendgridEmailParser>();
        }
    }
}
