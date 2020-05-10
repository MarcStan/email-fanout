using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IEmailService
    {
        /// <summary>
        /// Called with the body sent by sendgrid to determine which targets should receive the email.
        /// </summary>
        Task ProcessMailAsync(MemoryStream body, CancellationToken cancellationToken);
    }
}
