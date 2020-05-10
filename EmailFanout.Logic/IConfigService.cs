using EmailFanout.Logic.Config;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IConfigService
    {
        Task<EmailConfig> LoadAsync(CancellationToken cancellationToken);
    }
}
