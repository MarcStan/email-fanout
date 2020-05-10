using EmailFanout;
using EmailFanout.Logic;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
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

            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<ISendgridEmailParser, SendgridEmailParser>();
        }
    }
}
