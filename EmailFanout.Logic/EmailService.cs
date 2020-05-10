using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public class EmailService : IEmailService
    {
        private readonly IStatusService _statusService;
        private readonly IQueueService _queueService;

        public EmailService(
            IStatusService statusService,
            IQueueService queueService)
        {
            _statusService = statusService;
            _queueService = queueService;
        }

        public async Task ProcessMailAsync(MemoryStream body, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
