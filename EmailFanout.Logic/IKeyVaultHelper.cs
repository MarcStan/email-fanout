using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IKeyVaultHelper
    {
        Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken);
    }
}
