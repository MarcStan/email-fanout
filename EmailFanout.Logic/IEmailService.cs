using EmailFanout.Logic.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IEmailService
    {
        /// <summary>
        /// Called to forward the received mail to all targets.
        /// </summary>
        /// <returns>True if all actions succeeded, false otherwise.</returns>
        Task<bool> ProcessMailAsync(EmailRequest request, CancellationToken cancellationToken);
    }
}
