using EmailFanout.Logic.Config;
using EmailFanout.Logic.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IStatusService
    {
        Task<IReadOnlyDictionary<string, StatusModel>> GetStatiAsync(EmailRequest request, CancellationToken cancellationToken);

        Task<StatusModel> UpdateAsync(EmailRequest request, EmailAction action, EmailFanoutStatus status, CancellationToken cancellationToken);

        Task<StatusModel> UpdateAsync(EmailRequest request, EmailAction action, EmailFanoutStatus status, bool @override, CancellationToken cancellationToken);
    }
}
