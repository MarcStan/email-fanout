using EmailFanout.Logic.Config;
using EmailFanout.Logic.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic
{
    public interface IStatusService
    {
        Task<IReadOnlyDictionary<string, StatusModel>> GetStatusAsync(EmailRequest request, CancellationToken cancellationToken);

        Task<StatusModel> UpdateAsync(Email mail, EmailAction action, EmailFanoutStatus status, CancellationToken cancellationToken);
    }
}
